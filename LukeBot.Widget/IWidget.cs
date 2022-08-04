using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Config;


namespace LukeBot.Widget
{
    public abstract class IWidget
    {
        private struct WebSocketRecv
        {
            public ValueWebSocketReceiveResult result;
            public string data;

            public WebSocketRecv(ValueWebSocketReceiveResult result, string data)
            {
                this.result = result;
                this.data = data;
            }
        };

        public string ID { get; private set; }
        public string mWidgetFilePath;
        private List<string> mHead;
        protected WebSocket mWS;
        private ManualResetEvent mWSLifetimeEndEvent;
        private AutoResetEvent mWSRecvAvailableEvent;
        private Task mWSLifetimeTask;
        private Thread mWSMessagingThread;
        private bool mWSThreadDone;
        private Queue<string> mWSRecvQueue;

        protected event EventHandler OnConnectedEvent;


        private void OnConnected()
        {
            EventHandler handler = OnConnectedEvent;
            if (handler != null)
            {
                handler(this, null);
            }
        }

        private string GetWidgetCode()
        {
            if (!File.Exists(mWidgetFilePath))
                return "Widget code not found!";

            StreamReader reader = File.OpenText(mWidgetFilePath);
            string p = reader.ReadToEnd();
            reader.Close();

            return p;
        }

        private async Task<WebSocketRecv> RecvFromWSInternalAsync()
        {
            if (mWS.State != WebSocketState.Open)
                throw new WebSocketException("Web Socket is closed");

            string ret = "";
            ValueWebSocketReceiveResult recvResult;
            do
            {
                Memory<byte> buf = new Memory<byte>();
                recvResult = await mWS.ReceiveAsync(buf, CancellationToken.None);
                ret += Encoding.UTF8.GetString(buf.ToArray());
            }
            while (!recvResult.EndOfMessage);

            return new WebSocketRecv(recvResult, ret);
        }

        private async void WSRecvThreadMain()
        {
            mWSThreadDone = false;
            while (!mWSThreadDone)
            {
                WebSocketRecv recv = await RecvFromWSInternalAsync();

                if (recv.result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Log().Debug("Received close message");
                    mWSThreadDone = true;
                    mWSRecvAvailableEvent.Set();
                    continue;
                }

                Logger.Log().Debug("Enqueueing message");
                Logger.Log().Secure(" -> msg = {0}", recv.data);
                mWSRecvQueue.Enqueue(recv.data);
                mWSRecvAvailableEvent.Set();
            }

            CloseWS(WebSocketCloseStatus.NormalClosure);
        }


        protected void AddToHead(string line)
        {
            Logger.Log().Secure("Adding head line {0}", line);
            mHead.Add(line);
        }

        protected async void CloseWS(WebSocketCloseStatus status)
        {
            if (mWS != null && mWS.State == WebSocketState.Open)
                await mWS.CloseAsync(status, null, CancellationToken.None);

            mWSLifetimeEndEvent.Set(); // trigger Kestrel thread to finish the connection
        }

        protected string RecvFromWS()
        {
            while (mWSRecvQueue.Count == 0 && mWSThreadDone == false)
                mWSRecvAvailableEvent.WaitOne();

            if (mWSThreadDone)
                return "";

            return mWSRecvQueue.Dequeue();
        }

        protected async void SendToWSAsync(string msg)
        {
            if (mWS == null)
                return; // WebSocket not connected, ignore

            if (mWS.State == WebSocketState.Open)
            {
                await mWS.SendAsync(
                    Encoding.UTF8.GetBytes(msg).AsMemory<byte>(),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }


        internal Task AcquireWS(WebSocket ws)
        {
            if (mWSMessagingThread != null && mWSMessagingThread.IsAlive)
            {
                mWSThreadDone = true;
                CloseWS(WebSocketCloseStatus.NormalClosure);
                mWSMessagingThread.Join(); // Join the messaging thread
            }

            mWS = ws;
            mWSLifetimeEndEvent.Reset();
            mWSLifetimeTask = Task.Run(() => mWSLifetimeEndEvent.WaitOne());

            mWSMessagingThread = new Thread(WSRecvThreadMain);
            mWSMessagingThread.Start();

            OnConnected();
            return mWSLifetimeTask;
        }

        internal void SetID(string id)
        {
            ID = id;
        }


        public IWidget(string widgetFilePath)
        {
            mWidgetFilePath = widgetFilePath;

            ID = "";
            mHead = new List<string>();
            mWS = null;
            mWSLifetimeEndEvent = new ManualResetEvent(false);
            mWSRecvAvailableEvent = new AutoResetEvent(false);
            mWSThreadDone = false;
            mWSRecvQueue = new Queue<string>();
            mWSLifetimeTask = null;
        }

        public string GetPage()
        {
            string page = "<!DOCTYPE html><html><head>";

            // form head contents
            foreach (string h in mHead)
            {
                page += h;
            }

            string serverIP = Conf.Get<string>(Constants.SERVER_IP_FILE);
            page += string.Format("<meta name=\"serveraddress\" content=\"{0}\">", /* TODO PROPSTORE Utils.GetConfigServerIP() + */"/widgetws/" + ID);

            page += "</head><body>";
            page += GetWidgetCode();
            page += "</body></html>";

            return page;
        }

        public virtual void RequestShutdown()
        {
            mWSThreadDone = true;
            CloseWS(WebSocketCloseStatus.NormalClosure);
        }

        public virtual void WaitForShutdown()
        {
            if (mWSMessagingThread != null)
                mWSMessagingThread.Join();
        }
    }
}
