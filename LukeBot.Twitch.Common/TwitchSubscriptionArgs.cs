using System.Collections.Generic;
using LukeBot.Communication.Common;


namespace LukeBot.Twitch.Common
{
    public enum TwitchSubscriptionType
    {
        New = 0,
        Resubscription,
        Gift
    }

    public class TwitchSubscriptionDetails
    {
        public TwitchSubscriptionType Type { get; private set; }
        public int Tier { get; private set; }

        public TwitchSubscriptionDetails(string tier)
            : this(TwitchSubscriptionType.New, tier)
        {
        }

        internal TwitchSubscriptionDetails(TwitchSubscriptionType type, string tier)
        {
            Type = type;
            Tier = System.Int32.Parse(tier);
        }
    }

    public class TwitchResubscriptionDetails: TwitchSubscriptionDetails
    {
        public int Cumulative { get; private set; }
        public int Streak { get; private set; } // 0 if not shared
        public int Duration { get; private set; }
        // TODO resub message

        public TwitchResubscriptionDetails(string tier, int cumulative, int streak, int duration)
            : base(TwitchSubscriptionType.Resubscription, tier)
        {
            Cumulative = cumulative;
            Streak = streak;
            Duration = duration;
        }
    }

    public class TwitchGiftSubscriptionDetails: TwitchSubscriptionDetails
    {
        public int RecipentCount { get; private set; }

        public TwitchGiftSubscriptionDetails(string tier, int recipentCount)
            : base(TwitchSubscriptionType.Resubscription, tier)
        {
            RecipentCount = recipentCount;
        }
    }

    public class TwitchSubscriptionArgs: UserEventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchSubscriptionDetails Details { get; private set; }

        public TwitchSubscriptionArgs(string user, string displayName, TwitchSubscriptionDetails details)
            : base(UserEventType.TwitchSubscription, "SubscriptionEvent")
        {
            User = user;
            DisplayName = displayName;
            Details = details;
        }
    }
}