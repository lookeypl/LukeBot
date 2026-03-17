using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Twitch;


namespace LukeBot.Widget.Impl
{
    /**
     * Widget responsible for everything that could be considered an "Alert".
     *
     * Widget assumes events are processed one at a time. Once an event arrives,
     * Widget will internally queue them and execute one-after-another.
     */
    public class Alerts: QueueableEventWidget
    {
        private class AlertInterrupt: SerializableEventArgsBase
        {
            public AlertInterrupt()
                : base("AlertInterrupt")
            {
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<AlertInterrupt>(this);
            }
        }

        public class AlertTrigger
        {
            [ConfigurationListRestrictedField<string>(new[] { "sub", "giftsub", "cheer" })]
            private string EventType = "sub";
            [ConfigurationListRestrictedField<int>(new[] { 1, 2, 3 })]
            private int SubTier = 1;
            private int FromMonths = 0;
            private int ToMonths = 0;
        }

        public class AlertsConfig: Configuration<AlertsConfig>
        {
            [ConfigurationListRestrictedField<string>(new[] { "left", "right" })]
            private string Alignment = "right";
            [ConfigurationListRestrictedField<string>(new[] { "simple", "classic" })]
            private string Style = "simple";

            public AlertsConfig() {}
        }

        private void OnSimpleEvent<T>(object o, EventArgsBase args)
            where T : SerializableEventArgsBase
        {
            T a = args as T;
            SendEvent(a);
        }

        private void OnSubscriptionEvent(object o, EventArgsBase args)
        {
            TwitchSubscriptionArgs a = args as TwitchSubscriptionArgs;

            switch (a.Details.Type)
            {
            case TwitchSubscriptionType.New:
                Logger.Log().Debug("New sub from: {0} ({1}), tier {2}", a.User, a.DisplayName, a.Details.Tier);
                break;
            case TwitchSubscriptionType.Resub:
                TwitchResubscriptionDetails resub = a.Details as TwitchResubscriptionDetails;
                Logger.Log().Debug("Resub from: {0} ({1}), tier {2}, cumulative {3}, streak {4}, duration {5} msg {6}",
                    a.User, a.DisplayName, resub.Tier, resub.Cumulative, resub.Streak, resub.Duration, resub.Message);
                break;
            case TwitchSubscriptionType.Gift:
                TwitchGiftSubscriptionDetails gift = a.Details as TwitchGiftSubscriptionDetails;
                Logger.Log().Debug("Gift from: {0} ({1}), tier {2}, count {3}",
                    a.User, a.DisplayName, gift.Tier, gift.RecipentCount);
                break;
            }

            SendEvent(a);
        }

        private void SendConfiguration()
        {
            WidgetResponse response = SendEventAndWait(GetConfig());
            if (response == null) return; // quietly ignore, Widget not connected yet

            if (response.ErrorCount == 0)
            {
                Logger.Log().Info("Widget {0}: Configuration applied.", GetPrintableWidgetID());
            }
            else
            {
                LogResponseStatus(response);
            }
        }

        protected override void OnConnected()
        {
            EventSubscribe(Events.TWITCH_CHEER, OnSimpleEvent<TwitchCheerArgs>, true);
            EventSubscribe(Events.TWITCH_WATCH_STREAK, OnSimpleEvent<TwitchWatchStreakArgs>, true);
            EventSubscribe(Events.TWITCH_SUBSCRIPTION, OnSubscriptionEvent, true);

            SendConfiguration();
        }

        protected override void OnDisconnected()
        {
            EventUnsubscribe(Events.TWITCH_CHEER, OnSimpleEvent<TwitchCheerArgs>);
            EventUnsubscribe(Events.TWITCH_WATCH_STREAK, OnSimpleEvent<TwitchWatchStreakArgs>);
            EventUnsubscribe(Events.TWITCH_SUBSCRIPTION, OnSubscriptionEvent);
        }

        protected override void OnConfigurationUpdate()
        {
            SendConfiguration();
        }

        protected override void OnLoad()
        {
        }

        protected override void OnUnload()
        {

        }

        protected override ConfigurationBase CreateDefaultConfiguration()
        {
            return new AlertsConfig();
        }

        public Alerts(string lbUser, string id, string name)
            : base(lbUser, "Widgets/Alerts.html", id, name)
        {
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
