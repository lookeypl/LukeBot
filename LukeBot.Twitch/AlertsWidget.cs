using System.IO;
using System.Text.Json;
using LukeBot.Common;

namespace LukeBot.Twitch
{
    class AlertsWidget: IWidget
    {
        TwitchIRC mIRC;
        ConnectionPort mPort;
        WebSocketServer mServer;

        private void OnUSERNOTICE(object o, TwitchIRCUserNotice args)
        {
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(args));
        }

        public AlertsWidget(TwitchIRC IRC)
            : base()
        {
            mIRC = IRC;

            mPort = ConnectionManager.Instance.AcquirePort();
            Logger.Debug("Widget will have port {0}", mPort.Value);

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            WidgetManager.Instance.Register(this, "TEST-ALERTS-WIDGET");
            Logger.Secure("Registered Alerts widget at link http://{0}/widget/{1}; WS port {2}", serverIP, ID, mPort.Value);
        }

        ~AlertsWidget()
        {
        }

        protected override string GetWidgetCode()
        {
            if (mServer.Running)
            {
                mServer.RequestShutdown();
                mServer.WaitForShutdown();
            }

            StreamReader reader = File.OpenText("LukeBot.Twitch/Widgets/Alerts.html");
            string p = reader.ReadToEnd();
            reader.Close();

            mServer.Start();

            return p;
        }

        public override void RequestShutdown()
        {
            mServer.RequestShutdown();
        }

        public override void WaitForShutdown()
        {
            if (mServer.Running)
            {
                mServer.WaitForShutdown();
            }
        }
    }
}
