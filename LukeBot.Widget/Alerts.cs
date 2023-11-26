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
            Logger.Log().Debug("Channel Points redemption:\n{0}", msg);
            SendToWSAsync(msg);
        }

        public void OnSubscriptionEvent(object o, EventArgsBase args)
        {
            TwitchSubscriptionArgs a = args as TwitchSubscriptionArgs;

            switch (a.Details.Type)
            {
            case TwitchSubscriptionType.New:
                Logger.Log().Debug("New sub from: {0} ({1}), tier {2}", a.User, a.DisplayName, a.Details.Tier);
                break;
            case TwitchSubscriptionType.Resubscription:
                TwitchResubscriptionDetails resub = a.Details as TwitchResubscriptionDetails;
                Logger.Log().Debug("Resub from: {0} ({1}), tier {2}, cumulative {3}, streak {4}, duration {5}",
                    a.User, a.DisplayName, resub.Tier, resub.Cumulative, resub.Streak, resub.Duration);
                break;
            case TwitchSubscriptionType.Gift:
                TwitchGiftSubscriptionDetails gift = a.Details as TwitchGiftSubscriptionDetails;
                Logger.Log().Debug("Gift from: {0} ({1}), tier {2}, count {3}",
                    a.User, a.DisplayName, gift.Tier, gift.RecipentCount);
                break;
            }

            string msg = JsonConvert.SerializeObject(a);
            SendToWSAsync(msg);
        }

        public Alerts(string lbUser, string id, string name)
            : base("LukeBot.Widget/Widgets/Alerts.html", id, name)
        {
            Comms.Event.User(lbUser).TwitchChannelPointsRedemption += OnChannelPointsEvent;
            Comms.Event.User(lbUser).TwitchSubscription += OnSubscriptionEvent;
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
