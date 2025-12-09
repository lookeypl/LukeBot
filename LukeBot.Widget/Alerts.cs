using System.Collections.Generic;
using System.Runtime.Serialization;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;
using Newtonsoft.Json;


namespace LukeBot.Widget
{
    /**
     * Widget responsible for everything that could be considered an "Alert".
     *
     * Currently supported events
     *  - TwitchSubscription
     *
     * Widget assumes events come from a Queued Dispatcher and are processed
     * one at a time. Once an event arrives, Widget will block execution until
     * the JS side responds back with a WidgetEventCompletionResponse object.
     */
    public class Alerts: IWidget
    {
        private class AlertInterrupt: EventArgsBase
        {
            public AlertInterrupt()
                : base("AlertInterrupt")
            {
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

        private void AwaitEventCompletion()
        {
            try
            {
                if (!Connected)
                    return;

                WidgetEventCompletionResponse resp = RecvFromWS<WidgetEventCompletionResponse>();
                if (resp == null)
                {
                    Logger.Log().Warning("Widget's response was null - possibly connection was broken or is not connected");
                    return;
                }

                if (resp.ErrorCount == 0)
                {
                    Logger.Log().Debug("Widget completed event successfully");
                }
                else if (resp.ErrorCount == 1)
                {
                    Logger.Log().Warning("Widget failed to complete the event: {0}", resp.Reason[0]);
                }
                else
                {
                    Logger.Log().Warning("{0} errors occured during Widget's event completion attempt:", resp.ErrorCount);
                    for (int i = 0; i < resp.Reason.Length; ++i)
                    {
                        Logger.Log().Warning("  - {0}", resp.Reason[i]);
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0}: Caught {1} on Widget's receive loop: {2}",
                    Name, e.GetType().Name, e.Message
                );
            }
        }

        private void OnSimpleEvent<T>(object o, EventArgsBase args)
            where T : EventArgsBase
        {
            T a = args as T;
            SendToWS(a);
            AwaitEventCompletion();
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

            SendToWS(a, new TwitchSubscriptionArgsJsonConverter());

            AwaitEventCompletion();
        }

        private void OnEventInterrupt(object o, EventArgsBase args)
        {
            SendToWS(new AlertInterrupt());
        }

        protected override void OnConnected()
        {
            // must use internal serialization routine to use the proper Converter
            SendToWS(GetConfig().Serialize());
            AwaitEventCompletion();
        }

        protected override void OnConfigurationUpdate()
        {
            // must use internal serialization routine to use the proper Converter
            SendToWS(GetConfig().Serialize());
            AwaitEventCompletion();
        }

        protected override void OnLoad()
        {
            EventCollection collection = Comms.Event.User(mLBUser);

            collection.Event(Events.TWITCH_CHEER).Endpoint += OnSimpleEvent<TwitchCheerArgs>;
            collection.Event(Events.TWITCH_CHEER).InterruptEndpoint += OnEventInterrupt;

            collection.Event(Events.TWITCH_SUBSCRIPTION).Endpoint += OnSubscriptionEvent;
            collection.Event(Events.TWITCH_SUBSCRIPTION).InterruptEndpoint += OnEventInterrupt;
        }

        protected override void OnUnload()
        {
            EventCollection collection = Comms.Event.User(mLBUser);

            collection.Event(Events.TWITCH_CHEER).Endpoint -= OnSimpleEvent<TwitchCheerArgs>;
            collection.Event(Events.TWITCH_CHEER).InterruptEndpoint -= OnEventInterrupt;

            collection.Event(Events.TWITCH_SUBSCRIPTION).Endpoint -= OnSubscriptionEvent;
            collection.Event(Events.TWITCH_SUBSCRIPTION).InterruptEndpoint -= OnEventInterrupt;
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
