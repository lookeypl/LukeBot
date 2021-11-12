using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
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

        private struct PubSubListenCommand
        {
            public List<string> topics { get; private set; }
            public string auth_token { get; private set; }

            public PubSubListenCommand(string topic, string auth_token)
            {
                this.topics = new List<string>();
                this.topics.Add(topic);
                this.auth_token = auth_token;
            }
        }

        private struct PubSubCommand
        {
            public string type { get; private set; }
            public string nonce { get; private set; }
            public PubSubListenCommand data { get; private set; }

            public PubSubCommand(string type, PubSubListenCommand cmd)
            {
                this.type = type;
                data = cmd;

                using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
                {
                    byte[] nonceData = new byte[32];
                    rng.GetBytes(nonceData);
                    nonce = Convert.ToBase64String(nonceData);
                }
            }
        }

        private struct PubSubResponse
        {
            public string type { get; set; }
            public string error { get; set; }
            public string nonce { get; set; }
        }

        private struct PubSubMessageData
        {
            public string topic { get; set; }
            public string message { get; set; }

            public void Print(LogLevel logLevel)
            {
                Logger.Log().Message(logLevel, "   -> topic: {0}", topic);
                Logger.Log().Message(logLevel, "   -> message: {0}", message);
            }
        }

        private struct PubSubMessage
        {
            public string type { get; set; }
            public PubSubMessageData data { get; set; }

            public void Print(LogLevel logLevel)
            {
                Logger.Log().Message(logLevel, " -> type: {0}", type);
                Logger.Log().Message(logLevel, " -> data:");
                data.Print(logLevel);
            }
        }

        private struct PubSubReceiveStatus<T>
        {
            public bool closed { get; set; }
            public T obj { get; set; }
        }

        private Token mToken;
        private ClientWebSocket mSocket;
        private Thread mMainThread;
        private bool mDone;

        public event EventHandler<ChannelPointEventArgs> ChannelPointEvent;

        private async Task Send<T>(T obj)
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

        private async Task<PubSubReceiveStatus<T>> Receive<T>()
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

        private async void ThreadMain()
        {
            while (!mDone)
            {
                // TODO ping pong with twitch

                PubSubReceiveStatus<PubSubMessage> msg = await Receive<PubSubMessage>();
                if (msg.closed)
                    break;

                Logger.Log().Debug("Received message: ");
                msg.obj.Print(LogLevel.Debug);
            }
        }

        public PubSub(Token token)
        {
            mToken = token;
            mSocket = new ClientWebSocket();
            mMainThread = new Thread(ThreadMain);
            mDone = false;
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
                new PubSubListenCommand("channel-points-channel-v1." + user.data[0].id, mToken.Get())
            );
            await Send(command);
            PubSubReceiveStatus<PubSubResponse> r = await Receive<PubSubResponse>();
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

            mMainThread.Start();
            Logger.Log().Debug("Listening successfully started");
        }

        public void RequestShutdown()
        {
            mDone = true;
            if (mSocket.State == WebSocketState.Open)
            {
                CancellationToken cancelToken = new CancellationToken();
                mSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cancelToken);
            }
        }

        public void WaitForShutdown()
        {
            if (mMainThread.ThreadState != ThreadState.Unstarted)
                mMainThread.Join();
        }
    }
}
