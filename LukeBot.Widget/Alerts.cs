using Newtonsoft.Json;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class Alerts: IWidget
    {
        public void OnChannelPointsEvent(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = args as TwitchChannelPointsRedemptionArgs;
            string msg = JsonConvert.SerializeObject(a);
            Logger.Log().Debug("{0}", msg);
            SendToWSAsync(msg);
        }

        public void OnSubscriptionEvent(object o, EventArgsBase args)
        {
            TwitchSubscriptionArgs a = args as TwitchSubscriptionArgs;
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
