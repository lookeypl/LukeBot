using System.IO;
using System.Text.Json;
using LukeBot.Common;
using System.Net;


namespace LukeBot.Twitch
{
    class AlertsWidget: IWidget
    {
        ConnectionPort mPort;
        WebSocketServer mServer;


        public AlertsWidget()
            : base()
        {
            mPort = ConnectionManager.Instance.AcquirePort();
            Logger.Log().Debug("Widget will have port {0}", mPort.Value);

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            WidgetManager.Instance.Register(this, "TEST-ALERTS-WIDGET");
            Logger.Log().Secure("Registered Alerts widget at link http://{0}/widget/{1}; WS port {2}", serverIP, ID, mPort.Value);
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
