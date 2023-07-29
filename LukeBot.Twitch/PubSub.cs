using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Logging;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Communication.Events;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{
    public class PubSub: IEventPublisher
    {
        private const string PUBSUB_CHANNEL_POINTS_TOPIC = "channel-points-channel-v1";

        private struct PubSubReceiveStatus
        {
            public bool closed { get; set; }
            public PubSubMessage obj { get; set; }
        }

        // TODO all of this Channel Points stuff should be a separate "backend"
        private struct ChannelPointsUser
        {
            public string id { get; set; }
            public string login { get; set; }
            public string display_name { get; set; }
        }

        private struct ChannelPointsReward
        {
            public string title { get; set; }
        }

        private struct ChannelPointsRedemption
        {
            public ChannelPointsUser user { get; set; }
            public ChannelPointsReward reward { get; set; }
        }

        private struct ChannelPointsMessageData
        {
            public string timestamp { get; set; }
            public ChannelPointsRedemption redemption { get; set; }
        }

        private struct ChannelPointsMessage
        {
            public string type { get; set; }
            public ChannelPointsMessageData data { get; set; }
        }


        private string mLBUser;
        private Token mToken;
        private API.Twitch.GetUserData mUserData;
        private Uri mServerUri;
        private ClientWebSocket mSocket;
        private Thread mReceiveThread;
        private Thread mSendThread;
        private Queue<PubSubMessage> mSendQueue;
        private AutoResetEvent mSendQueueEvent;
        private Mutex mSendQueueMutex;
        private System.Timers.Timer mPingPongTimer;
        private bool mDone;
        private EventCallback mChannelPointsRedemptionCallback;

        private async Task SocketSend<T>(T obj)
        {
            byte[] msgJsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));

            CancellationToken cancelToken = new CancellationToken();
            await mSocket.SendAsync(
                new ArraySegment<byte>(msgJsonBytes),
                WebSocketMessageType.Text,
                true,
                cancelToken
            );
        }

        private async void Reconnect()
        {
            if (mSocket.State == WebSocketState.Open)
            {
                CancellationToken cancellationToken = new CancellationToken();
                await mSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
            }

            // mSocket has to be recreated here - CloseAsync() puts it in 'Closed' state
            // which by C# standards is illegal to call ConnectAsync() on
            mSocket = new ClientWebSocket();
            Connect(mServerUri);
        }

        private async Task<PubSubReceiveStatus> SocketReceive()
        {
            string recvMsgString = "";
            byte[] buffer = new byte[1024];

            PubSubReceiveStatus result = new PubSubReceiveStatus();
            CancellationToken cancelToken = new CancellationToken();
            WebSocketReceiveResult recvResult;
            do
            {
                recvResult = await mSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                recvMsgString += Encoding.UTF8.GetString(buffer, 0, recvResult.Count);
            }
            while (!recvResult.EndOfMessage);

            if (recvResult.MessageType == WebSocketMessageType.Close)
            {
                result.closed = true;
                return result;
            }

            result.closed = false;

            PubSubMessage baseMsg = JsonConvert.DeserializeObject<PubSubMessage>(recvMsgString);
            switch (baseMsg.type)
            {
            case PubSubMsgType.RESPONSE:
                result.obj = JsonConvert.DeserializeObject<PubSubResponse>(recvMsgString);
                break;
            case PubSubMsgType.MESSAGE:
                result.obj = JsonConvert.DeserializeObject<PubSubTopicMessage>(
                    recvMsgString, new PubSubMessageDataCreationConverter()
                );
                break;
            default:
                result.obj = baseMsg;
                break;
            }

            return result;
        }

        private void HandleChannelPointsEvent(string message)
        {
            ChannelPointsMessage cpMsg = JsonConvert.DeserializeObject<ChannelPointsMessage>(message);
            mChannelPointsRedemptionCallback.PublishEvent(
                new TwitchChannelPointsRedemptionArgs(
                    cpMsg.data.redemption.user.login,
                    cpMsg.data.redemption.user.display_name,
                    cpMsg.data.redemption.reward.title
                )
            );
        }

        private void ProcessReceivedMessageData(PubSubReceivedMessageData data)
        {
            Logger.Log().Debug("  Message topic: {0}", data.topic);
            string[] topic = data.topic.Split('.');
            switch (topic[0])
            {
            case PUBSUB_CHANNEL_POINTS_TOPIC:
                HandleChannelPointsEvent(data.message);
                break;
            default:
                Logger.Log().Warning("Skipping unsupported PubSub topic: {0}", topic[0]);
                break;
            }
        }

        private async void ReceiveThreadMain()
        {
            while (!mDone)
            {
                try
                {
                    PubSubReceiveStatus msg = await SocketReceive();
                    if (msg.closed)
                        break;

                    Logger.Log().Debug("Received message: ");
                    PubSubMessage m = msg.obj;
                    m.Print(LogLevel.Debug);

                    switch (m.type)
                    {
                    case PubSubMsgType.MESSAGE:
                        PubSubTopicMessage tm = (PubSubTopicMessage)m;
                        PubSubReceivedMessageData rmData = (PubSubReceivedMessageData)tm.data;
                        ProcessReceivedMessageData(rmData);
                        break;
                    case PubSubMsgType.RECONNECT:
                        Logger.Log().Warning("RECONNECT message received - attempting reconnect...");
                        Reconnect();
                        Logger.Log().Warning("Reconnected");
                        break;
                    default:
                        continue;
                    }
                }
                catch (WebSocketException wse)
                {
                    Logger.Log().Warning("WebSocketException caught: {0}", wse.Message);
                    Logger.Log().Warning("Attempting to reconnect...");
                    Reconnect();
                }
            }
        }

        private async void SendThreadMain()
        {
            while (!mDone)
            {
                mSendQueueEvent.WaitOne();

                mSendQueueMutex.WaitOne();

                if (mSendQueue.Count == 0)
                    continue;

                PubSubMessage c = mSendQueue.Dequeue();
                await SocketSend(c);

                mSendQueueMutex.ReleaseMutex();
            }
        }

        private void Send(PubSubMessage c)
        {
            mSendQueueMutex.WaitOne();

            mSendQueue.Enqueue(c);
            mSendQueueEvent.Set();

            mSendQueueMutex.ReleaseMutex();
        }

        private void OnPingPongTimerEvent(Object o, ElapsedEventArgs e)
        {
            Send(new PubSubMessage(PubSubMsgType.PING));
        }

        public PubSub(string lbUser, Token token, API.Twitch.GetUserData userData)
        {
            mLBUser = lbUser;
            mToken = token;
            mUserData = userData;
            mSocket = new ClientWebSocket();
            mDone = false;

            mReceiveThread = new Thread(ReceiveThreadMain);

            mSendQueue = new Queue<PubSubMessage>();
            mSendQueueEvent = new AutoResetEvent(false);
            mSendQueueMutex = new Mutex();
            mPingPongTimer = new System.Timers.Timer();
            mPingPongTimer.Interval = 5 * 60 * 1000;
            mPingPongTimer.Elapsed += OnPingPongTimerEvent;
            mSendThread = new Thread(SendThreadMain);
            mSendThread.Name = string.Format("PubSub Send Thread ({0})", mUserData.login);

            if (Comms.Initialized)
            {
                List<EventCallback> events = Comms.Event.User(mLBUser).RegisterEventPublisher(
                    this, UserEventType.TwitchChannelPointsRedemption
                );

                foreach (EventCallback e in events)
                {
                    switch (e.userType)
                    {
                    case UserEventType.TwitchChannelPointsRedemption:
                        mChannelPointsRedemptionCallback = e;
                        break;
                    default:
                        Logger.Log().Warning("Received unknown event type from Event system");
                        break;
                    }
                }
            }
        }

        ~PubSub()
        {
        }

        public async void Connect(Uri address)
        {
            CancellationToken cancelToken = new CancellationToken();
            if (mSocket.State == WebSocketState.None)
            {
                await mSocket.ConnectAsync(address, cancelToken);
            }

            // mostly a workaround for tests to not generate auth tokens for no reason
            string tokenValue = mToken != null ? mToken.Get() : "";

            PubSubCommand command = new PubSubCommand(
                PubSubMsgType.LISTEN,
                new PubSubListenCommandData(
                    PUBSUB_CHANNEL_POINTS_TOPIC + '.' + mUserData.id,
                    tokenValue
                )
            );
            await SocketSend(command);
            PubSubReceiveStatus r = await SocketReceive();
            if (r.closed)
            {
                Logger.Log().Error("Connection closed while waiting for response");
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            if (r.obj.type != PubSubMsgType.RESPONSE)
            {
                Logger.Log().Error("Invalid response type received");
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            PubSubResponse resp = (PubSubResponse)r.obj;
            if (resp.nonce != command.nonce)
            {
                Logger.Log().Error("VERY BAD nonce is not the same bad twitch slap");
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            if (resp.error.Length > 0)
            {
                Logger.Log().Error("Error after command was sent to PubSub: {0}", resp.error);
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            mServerUri = address;
            mReceiveThread.Start();
            mSendThread.Start();
            mPingPongTimer.Enabled = true;
            Logger.Log().Debug("PubSub listening successfully started");
        }

        public void RequestShutdown()
        {
            mDone = true;
            mSendQueueEvent.Set();
            if (mSocket.State == WebSocketState.Open)
            {
                CancellationToken cancelToken = new CancellationToken();
                mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
            }
        }

        public void WaitForShutdown()
        {
            if (mReceiveThread.ThreadState != ThreadState.Unstarted)
                mReceiveThread.Join();

            if (mSendThread.ThreadState != ThreadState.Unstarted)
                mSendThread.Join();
        }
    }
}
