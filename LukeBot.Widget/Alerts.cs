using Newtonsoft.Json;
using LukeBot.Communication;
using LukeBot.Communication.Events;
using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class Alerts: IWidget
    {
        public void OnChannelPointsEvent(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = (TwitchChannelPointsRedemptionArgs)args;
            string msg = JsonConvert.SerializeObject(a);
            Logger.Log().Debug("{0}", msg);
            SendToWSAsync(msg);
        }

        public Alerts(string lbUser, string id, string name)
            : base("LukeBot.Widget/Widgets/Alerts.html", id, name)
        {
            Comms.Event.User(lbUser).TwitchChannelPointsRedemption += OnChannelPointsEvent;
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.alerts;
        }

        ~Alerts()
        {
        }
    }
}
