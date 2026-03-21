using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Common;
using System.Text.Json.Serialization;


namespace LukeBot.Widget.Impl
{
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

        protected class WidgetResponse
        {
            public Guid EventID { get; set; }
            public int ErrorCount { get; set; }
            public string[] Reason { get; set; }
        }

        public string ID { get; private set; }
        public string Name { get; private set; }
        public bool Loaded { get; private set; }
        public string mWidgetFilePath;
        protected string mLBUser;
        private List<string> mHead;
        protected WebSocket mWS;
        private ConfigurationBase mConfiguration;
        private ManualResetEvent mWSLifetimeEndEvent;
        private Task mWSLifetimeTask;
        private Thread mWSMessagingThread;
        private bool mWSThreadDone;
        private Config.Path mConfigurationPath;
        private Config.Path mBackupConfigurationPath;

        /**
         * Check if Widget is Connected to the JS-side. Returns true if WebSocket is established and is in Open state.
         */
        protected bool Connected { get { return mWS != null && mWS.State == WebSocketState.Open; } }

        /**
         * Called when Widget is loaded by the service. This does not mean Widget is connected, rather that
         * it has been spawned on the server-side.
         *
         * An Exception can be thrown from this method which will leave the Widget in unloaded state.
         *
         * To detect when client-side of the Widget is connected, override OnConnected().
         */
        protected virtual void OnLoad() { }

        /**
         * Called when Widget is unloaded by the service.
         */
        protected virtual void OnUnload() { }

        /**
         * Called when client-side connects to the Widget.
         *
         * At this point WebSocket connection is established, so using functions like @p SendToWS()
         * is possible. Receive Thread will also emit @p OnReceivedResponse() callbacks when it
         * picks up a client-side message.
         */
        protected virtual void OnConnected() { }

        /**
         * Called when client-side disconnects. Assume that by this point WebSocket is already
         * disconnected and no communication can be made with client-side.
         */
        protected virtual void OnDisconnected() { }

        /**
         * Creates Widget's default configuration when loading and initializing it.
         *
         * This will be called if there is no configuration present in LukeBot's properties.
         */
        protected abstract ConfigurationBase CreateDefaultConfiguration();

        /**
         * Called when Configuration is updated ex. by CLI.
         *
         * Use this override to apply the Configuration update to client-side.
         */
        protected virtual void OnConfigurationUpdate() { }

        /**
         * Called when Receive Thread picks up a message from client-side. Override this method
         * to process the response.
         *
         * NOTE: This is called by the Receive Thread. As such, any blocking communication
         * (ex. expecting another Response) must be handled manually (or better, not handled at
         * all).
         */
        protected virtual void OnReceivedResponse(WidgetResponse response) { }

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
            int port = LukeBot.Common.Constants.DEFAULT_SERVER_PORT;

            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP);
            if (Conf.TryGet<int>(LukeBot.Common.Constants.PROP_STORE_SERVER_PORT_PROP, out int gotPort))
            {
                port = gotPort;
            }

            return String.Format("https://{0}:{1}/widget/{2}", serverAddress, port, ID);
        }

        private string GetWidgetWSAddress()
        {
            int port = LukeBot.Common.Constants.DEFAULT_SERVER_PORT;

            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP);
            if (Conf.TryGet<int>(LukeBot.Common.Constants.PROP_STORE_SERVER_PORT_PROP, out int gotPort))
            {
                port = gotPort;
            }

            return String.Format("wss://{0}:{1}/widgetws/{2}", serverAddress, port, ID);
        }

        internal string GetPrintableWidgetID()
        {
            if (Name.Length > 0) return Name;
            else return ID;
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
                    ret += Encoding.UTF8.GetString(buffer, 0, recvResult.Count);
                }
            }
            while (!recvResult.EndOfMessage);

            return new WebSocketRecv(recvResult, ret);
        }

        private async void WSRecvThreadMain()
        {
            try
            {
                mWSThreadDone = false;
                while (!mWSThreadDone)
                {
                    WebSocketRecv recv = await RecvFromWSInternalAsync();

                    if (recv.result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.Log().Debug("Received close message");
                        mWSThreadDone = true;
                        continue;
                    }

                    Logger.Log().Debug("{0}: Received message", GetPrintableWidgetID());
                    Logger.Log().Secure("{0}:  -> msg = {1}", GetPrintableWidgetID(), recv.data);
                    OnReceivedResponse(JsonSerializer.Deserialize<WidgetResponse>(recv.data));
                }

                CloseWS(WebSocketCloseStatus.NormalClosure);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Widget {0}: Receive thread raised an Exception: {1}", Name, e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);

                CloseWS(WebSocketCloseStatus.InternalServerError);
            }

            OnDisconnected();
        }

        private async Task<bool> SendToWSAsync(string msg)
        {
            if (mWS == null || mWS.State != WebSocketState.Open)
                return false; // WebSocket not connected, ignore

            await mWS.SendAsync(
                Encoding.UTF8.GetBytes(msg).AsMemory<byte>(),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            return true;
        }

        private bool SendToWS(string msg)
        {
            Task<bool> t = SendToWSAsync(msg);
            t.Wait();
            return t.Result;
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

        protected async Task<bool> SendToWSAsync<T>(T obj)
            where T: SerializableEventArgsBase
        {
            return await SendToWSAsync(obj.Serialize());
        }

        protected bool SendToWS<T>(T obj)
            where T: SerializableEventArgsBase
        {
            Task<bool> t = SendToWSAsync(obj);
            t.Wait();
            return t.Result;
        }

        protected void LoadConfiguration()
        {
            if (Conf.TryGet<string>(mConfigurationPath, out string configStr))
            {
                mConfiguration = ConfigurationFactory.Deserialize(configStr);
            }
            else
            {
                mConfiguration = CreateDefaultConfiguration();
            }

            // Add the UpdateNotifier and afterwards manually trigger the Configuration update
            mConfiguration.UpdateNotifier = OnConfigurationUpdate;
            OnConfigurationUpdate();
        }


        public void SaveConfiguration()
        {
            if (mConfiguration.EventName == Constants.EMPTY_WIDGET_CONFIGURATION_NAME)
                return; // skip saving config for widgets with no config

            string widgetConfigStr = mConfiguration.Serialize();

            if (Conf.Exists(mConfigurationPath))
                Conf.Modify<string>(mConfigurationPath, widgetConfigStr);
            else
                Conf.Add(mConfigurationPath, Property.Create<string>(widgetConfigStr));
            Conf.Save();
        }

        public void ResetConfiguration()
        {
            // backup old configuration in case it needs to be looked at again (mostly for debugging)
            if (Conf.Exists(mConfigurationPath))
            {
                if (Conf.Exists(mBackupConfigurationPath))
                    Conf.Remove(mBackupConfigurationPath);

                Conf.Copy(mConfigurationPath, mBackupConfigurationPath);
            }

            mConfiguration = CreateDefaultConfiguration();
            SaveConfiguration();
        }

        public void PushConfigurationUpdate()
        {
            OnConfigurationUpdate();
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

            mWSMessagingThread = new(WSRecvThreadMain);
            mWSMessagingThread.Name = "Widget WS Thread (" + mLBUser + ")";
            mWSMessagingThread.Start();

            OnConnected();
            return mWSLifetimeTask;
        }


        protected IWidget(string lbUser, string widgetFilePath, string id, string name)
        {
            mWidgetFilePath = widgetFilePath;

            ID = id;
            Name = name;
            Loaded = false;
            mLBUser = lbUser;
            mHead = new List<string>();
            mWS = null;
            mWSLifetimeEndEvent = new ManualResetEvent(false);
            mWSThreadDone = false;
            mWSLifetimeTask = null;
            mConfigurationPath = Config.Path.Start()
                .Push(Constants.PROP_STORE_WIDGET_DOMAIN)
                .Push(mLBUser)
                .Push(ID)
                .Push(Constants.PROP_CONFIG);
            mBackupConfigurationPath = Config.Path.Start()
                .Push(Constants.PROP_STORE_WIDGET_DOMAIN)
                .Push(mLBUser)
                .Push(ID)
                .Push(Constants.PROP_CONFIG_BACKUP);
            mConfiguration = new EmptyWidgetConfiguration();
        }

        public void Load()
        {
            if (Loaded)
                return;

            LoadConfiguration();
            OnLoad();
            Loaded = true;
        }

        public void Unload()
        {
            if (!Loaded)
                return;

            mWSThreadDone = true;
            CloseWS(WebSocketCloseStatus.NormalClosure);

            if (mWSMessagingThread != null)
                mWSMessagingThread.Join();

            SaveConfiguration();

            OnUnload();
            Loaded = false;
        }

        public string GetPage()
        {
            string page = "<!DOCTYPE html><html><head>";

            // form head contents
            foreach (string h in mHead)
            {
                page += h;
            }

            page += String.Format("<meta name=\"serveraddress\" content=\"{0}\">", GetWidgetWSAddress());
            page += String.Format("<title>{0} LukeBot Widget - {1}</title>", Utils.Capitalize(GetWidgetType().ToString()), mLBUser);

            page += "</head><body>";
            page += GetWidgetCode();
            page += "</body></html>";

            return page;
        }

        public ConfigurationBase GetConfig()
        {
            return mConfiguration;
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
    }
}
