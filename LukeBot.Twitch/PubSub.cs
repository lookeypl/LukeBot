using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Auth;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{
    class PubSub
    {
        public struct ChannelPointEventArgs
        {
            // ...
        }

        private class PubSubMessage
        {
            public string type { get; set; }

            public PubSubMessage(string cmdType)
            {
                type = cmdType;
            }

            public void Print(LogLevel level)
            {
                Logger.Log().Message(level, " -> type: {0}", type);
            }
        }

        private abstract class PubSubMessageData
        {
            public abstract void Print(LogLevel level);
        }

        private class PubSubCommand: PubSubMessage
        {
            public string nonce { get; private set; }
            public PubSubMessageData data { get; private set; }

            public PubSubCommand(string cmdType, PubSubMessageData cmdData)
                : base(cmdType)
            {
                using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
                {
                    byte[] nonceData = new byte[32];
                    rng.GetBytes(nonceData);
                    nonce = Convert.ToBase64String(nonceData);
                }

                data = cmdData;
            }
        }

        private class PubSubListenCommandData: PubSubMessageData
        {
            public List<string> topics { get; private set; }
            public string auth_token { get; private set; }

            public PubSubListenCommandData(string topic, string authToken)
            {
                topics = new List<string>();
                topics.Add(topic);
                auth_token = authToken;
            }

            public override void Print(LogLevel level)
            {
                Logger.Log().Secure("   -> auth_token: {0}", auth_token);
                Logger.Log().Message(level, "   -> topics:", topics);
                foreach (string t in topics)
                {
                    Logger.Log().Message(level, "     -> {0}", t);
                }
            }
        }

        private class PubSubResponse: PubSubMessage
        {
            public string error { get; set; }
            public string nonce { get; set; }

            public PubSubResponse(string type)
                : base(type)
            {
            }
        }

        private class PubSubReceivedMessageData: PubSubMessageData
        {
            public string topic { get; set; }
            public string message { get; set; }

            public override void Print(LogLevel logLevel)
            {
                Logger.Log().Message(logLevel, "   -> topic: {0}", topic);
                Logger.Log().Message(logLevel, "   -> message: {0}", message);
            }
        }

        private class PubSubTopicMessage: PubSubMessage
        {
            public PubSubMessageData data { get; set; }

            public PubSubTopicMessage(string type)
                : base(type)
            {
                data = null;
            }

            public new void Print(LogLevel level)
            {
                base.Print(level);
                Logger.Log().Message(level, " -> data:");
                data.Print(level);
            }
        }


        private struct PubSubReceiveStatus<T>
        {
            public bool closed { get; set; }
            public T obj { get; set; }
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

        public event EventHandler<ChannelPointEventArgs> ChannelPointEvent;

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

        private async Task<PubSubReceiveStatus<T>> SocketReceive<T>()
        {
            string recvMsgString = "";
            byte[] buffer = new byte[1024];

            PubSubReceiveStatus<T> result = new PubSubReceiveStatus<T>();
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
            result.obj = JsonConvert.DeserializeObject<T>(recvMsgString);
            return result;
        }

        private async void ReceiveThreadMain()
        {
            while (!mDone)
            {
                PubSubReceiveStatus<PubSubMessage> msg = await SocketReceive<PubSubMessage>();
                if (msg.closed)
                    break;

                Logger.Log().Debug("Received message: ");
                msg.obj.Print(LogLevel.Debug);
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
            PubSubReceiveStatus<PubSubResponse> r = await SocketReceive<PubSubResponse>();
            if (r.closed)
            {
                Logger.Log().Error("Connection closed while waiting for response");
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            if (r.obj.nonce != command.nonce)
            {
                Logger.Log().Error("VERY BAD nonce is not the same bad twitch slap");
                await mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
                return;
            }

            if (r.obj.error.Length > 0)
            {
                Logger.Log().Error("Error after command was sent to PubSub: {0}", r.obj.error);
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
