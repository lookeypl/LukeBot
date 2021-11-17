using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Auth;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{
    public struct ChannelPointsEventArgs
    {
        public string Type { get; private set; }
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public string Title { get; private set; }

        public ChannelPointsEventArgs(string user, string displayName, string title)
        {
            Type = "ChannelPointsEvent";
            User = user;
            DisplayName = displayName;
            Title = title;
        }
    }

    class PubSub
    {
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


        private Token mToken;
        private ClientWebSocket mSocket;
        private Thread mReceiveThread;
        private Thread mSendThread;
        private Queue<PubSubMessage> mSendQueue;
        private AutoResetEvent mSendQueueEvent;
        private Mutex mSendQueueMutex;
        private System.Timers.Timer mPingPongTimer;
        private bool mDone;

        public event EventHandler<ChannelPointsEventArgs> ChannelPointsEvent;

        public void OnChannelPointsEvent(ChannelPointsEventArgs args)
        {
            EventHandler<ChannelPointsEventArgs> handler = ChannelPointsEvent;
            if (handler != null)
                handler(this, args);
        }

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
            case "RESPONSE":
                result.obj = JsonConvert.DeserializeObject<PubSubResponse>(recvMsgString);
                break;
            case "MESSAGE":
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

        private void ProcessReceivedMessageData(PubSubReceivedMessageData data)
        {
            Logger.Log().Debug("  Message topic: {0}", data.topic);
            string[] topic = data.topic.Split('.');
            switch (topic[0])
            {
            case "channel-points-channel-v1":
                ChannelPointsMessage cpMsg = JsonConvert.DeserializeObject<ChannelPointsMessage>(data.message);
                OnChannelPointsEvent(
                    new ChannelPointsEventArgs(
                        cpMsg.data.redemption.user.login,
                        cpMsg.data.redemption.user.display_name,
                        cpMsg.data.redemption.reward.title
                    )
                );
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
                PubSubReceiveStatus msg = await SocketReceive();
                if (msg.closed)
                    break;

                Logger.Log().Debug("Received message: ");
                PubSubMessage m = msg.obj;
                m.Print(LogLevel.Debug);

                switch (m.type)
                {
                case "MESSAGE":
                    PubSubTopicMessage tm = (PubSubTopicMessage)m;
                    PubSubReceivedMessageData rmData = (PubSubReceivedMessageData)tm.data;
                    ProcessReceivedMessageData(rmData);
                    break;
                default:
                    // TODO this also handles PONG and RECONNECT, which probably should be handled better
                    continue;
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
            Send(new PubSubMessage("PING"));
        }

        public PubSub(Token token)
        {
            mToken = token;
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
        }

        ~PubSub()
        {
        }

        public async void Listen(API.GetUserResponse user)
        {
            CancellationToken cancelToken = new CancellationToken();
            if (mSocket.State == WebSocketState.None)
            {
                await mSocket.ConnectAsync(new Uri("wss://pubsub-edge.twitch.tv"), cancelToken);
            }

            PubSubCommand command = new PubSubCommand(
                "LISTEN",
                new PubSubListenCommandData(
                    "channel-points-channel-v1." + user.data[0].id,
                    mToken.Get()
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

            if (r.obj.type != "RESPONSE")
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

            mReceiveThread.Start();
            mSendThread.Start();
            mPingPongTimer.Enabled = true;
            Logger.Log().Debug("Listening successfully started");
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
