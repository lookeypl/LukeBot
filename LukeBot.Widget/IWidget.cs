using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    internal class WidgetEventCompletionResponse
    {
        public int Status { get; set; }
        public string Reason { get; set; }
    }

    public abstract class IWidget
    {
        private struct WebSocketRecv
        {
            public WebSocketReceiveResult result;
            public string data;

            public WebSocketRecv(WebSocketReceiveResult result, string data)
            {
                this.result = result;
                this.data = data;
            }
        };

        public string ID { get; private set; }
        public string Name { get; private set; }
        public string mWidgetFilePath;
        private List<string> mHead;
        protected WebSocket mWS;
        private ManualResetEvent mWSLifetimeEndEvent;
        private AutoResetEvent mWSRecvAvailableEvent;
        private Task mWSLifetimeTask;
        private Thread mWSMessagingThread;
        private bool mWSThreadDone;
        private Queue<string> mWSRecvQueue;

        protected abstract void OnConnected();

        private string GetWidgetCode()
        {
            if (!File.Exists(mWidgetFilePath))
                return "Widget code not found!";

            StreamReader reader = File.OpenText(mWidgetFilePath);
            string p = reader.ReadToEnd();
            reader.Close();

            return p;
        }

        internal string GetWidgetAddress()
        {
            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_SERVER_IP_PROP);
            // TODO HTTP/HTTPS????
            return "http://" + serverAddress + "/widget/" + ID;
        }

        private string GetWidgetWSAddress()
        {
            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_SERVER_IP_PROP);
            // TODO WS/WSS????
            return "ws://" + serverAddress + "/widgetws/" + ID;
        }

        private async Task<WebSocketRecv> RecvFromWSInternalAsync()
        {
            if (mWS.State != WebSocketState.Open)
                throw new WebSocketException("Web Socket is closed");

            string ret = "";
            WebSocketReceiveResult recvResult;
            byte[] buffer = new byte[1024];
            do
            {
                ArraySegment<byte> buf = new(buffer);
                recvResult = await mWS.ReceiveAsync(buf, CancellationToken.None);
                if (recvResult.MessageType == WebSocketMessageType.Text)
                {
                    ret += Encoding.UTF8.GetString(buf);
                }
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
            if (mWS == null || mWS.State != WebSocketState.Open)
                return null;

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


        public IWidget(string widgetFilePath, string id, string name)
        {
            mWidgetFilePath = widgetFilePath;

            ID = id;
            Name = name;
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

            page += string.Format("<meta name=\"serveraddress\" content=\"{0}\">", GetWidgetWSAddress());

            page += "</head><body>";
            page += GetWidgetCode();
            page += "</body></html>";

            return page;
        }

        public WidgetDesc GetDesc()
        {
            WidgetDesc wd = new WidgetDesc();

            wd.Type = GetWidgetType();
            wd.Id = ID;
            wd.Name = Name;
            wd.Address = GetWidgetAddress();

            return wd;
        }

        public abstract WidgetType GetWidgetType();

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
