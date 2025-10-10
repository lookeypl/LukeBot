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
        public bool Loaded { get; private set; }
        public string mWidgetFilePath;
        protected string mLBUser;
        private List<string> mHead;
        protected WebSocket mWS;
        private ConfigurationBase mConfiguration;
        private ManualResetEvent mWSLifetimeEndEvent;
        private AutoResetEvent mWSRecvAvailableEvent;
        private Task mWSLifetimeTask;
        private Thread mWSMessagingThread;
        private bool mWSThreadDone;
        private Queue<string> mWSRecvQueue;
        private Config.Path mConfigurationPath;
        private Config.Path mBackupConfigurationPath;

        protected bool Connected { get { return mWS != null && mWS.State == WebSocketState.Open; } }
        protected abstract void OnLoad(); // called when widget is loaded. Can throw, which will leave widget in unloaded state.
        protected abstract void OnUnload(); // called when widget is loaded. Can throw, which will leave widget in unloaded state.
        protected abstract void OnConnected();
        protected abstract ConfigurationBase CreateDefaultConfiguration();
        protected virtual void OnConfigurationUpdate() { }

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
            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP);
            return "https://" + serverAddress + "/widget/" + ID;
        }

        private string GetWidgetWSAddress()
        {
            string serverAddress = Conf.Get<string>(LukeBot.Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP);
            return "wss://" + serverAddress + "/widgetws/" + ID;
        }

        private string GetPrintableWidgetID()
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
                        mWSRecvAvailableEvent.Set();
                        continue;
                    }

                    Logger.Log().Debug("{0}: Enqueueing message", GetPrintableWidgetID());
                    Logger.Log().Secure("{0}:  -> msg = {1}", GetPrintableWidgetID(), recv.data);
                    mWSRecvQueue.Enqueue(recv.data);
                    mWSRecvAvailableEvent.Set();
                }

                CloseWS(WebSocketCloseStatus.NormalClosure);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Widget {0}: Receive thread raised an Exception: {1}", Name, e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);

                CloseWS(WebSocketCloseStatus.InternalServerError);
            }
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

        protected T RecvFromWS<T>()
        {
            return JsonSerializer.Deserialize<T>(RecvFromWS());
        }

        protected async Task SendToWSAsync(string msg)
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

        protected async Task SendToWSAsync<T>(T obj)
        {
            await SendToWSAsync(JsonSerializer.Serialize(obj));
        }

        protected void SendToWS(string msg)
        {
            Task t = SendToWSAsync(msg);
            t.Wait();
        }

        protected void SendToWS<T>(T obj)
        {
            Task t = SendToWSAsync<T>(obj);
            t.Wait();
        }

        protected void LoadConfiguration()
        {
            try
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
            catch (ConfigurationException e)
            {
                Logger.Log().Error("Failed to load {0} Widget's configuration. This might be because it is either old or becuase of some other error.");
                Logger.Log().Error("If you're okay with losing the configuration data, try calling below CLI command to recreate it:");
                Logger.Log().Error("  widget reload {0} --recreate-config", GetPrintableWidgetID());
                Logger.Log().Error("Old configuration will be backed up in config for cross-reference.");
                #pragma warning disable CA2200
                throw e;
                #pragma warning restore CA2200
            }
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
            mWSRecvAvailableEvent = new AutoResetEvent(false);
            mWSThreadDone = false;
            mWSRecvQueue = new Queue<string>();
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

            page += string.Format("<meta name=\"serveraddress\" content=\"{0}\">", GetWidgetWSAddress());

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
