using System.IO;
using LukeBot.Common;
using Newtonsoft.Json;
using LukeBot.Core;
using LukeBot.Core.Events;


namespace LukeBot.Twitch
{
    class AlertsWidget: IWidget
    {
        ConnectionPort mPort;
        WebSocketServer mServer;


        public void OnChannelPointsEvent(object o, EventArgsBase args)
        {
            if (mServer.Running)
            {
                TwitchChannelPointsRedemptionArgs a = (TwitchChannelPointsRedemptionArgs)args;
                string msg = JsonConvert.SerializeObject(a);
                Logger.Log().Debug("{0}", msg);
                mServer.Send(msg);
            }
        }

        public AlertsWidget()
            : base()
        {
            mPort = Systems.Connection.AcquirePort();
            Logger.Log().Debug("Widget will have port {0}", mPort.Value);

            Systems.Event.TwitchChannelPointsRedemption += OnChannelPointsEvent;

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            Systems.Widget.Register(this, "TEST-ALERTS-WIDGET");
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
