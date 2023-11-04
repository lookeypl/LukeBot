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

    public class TwitchSubscriptionDetail
    {
        public int Tier { get; set; }
        public int Streak { get; set; }
        TwitchSubscriptionType Type { get; set; }
    }

    public class TwitchResubscriptionDetail: TwitchSubscriptionDetail
    {
        public int Months { get; set; }
    }

    public class TwitchGiftSubscriptionDetail: TwitchSubscriptionDetail
    {
        public List<string> Recipents { get; set; }
    }

    public class TwitchSubscriptionArgs: UserEventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchSubscriptionDetail Details { get; set; }

        public TwitchSubscriptionArgs(string user, string displayName)
            : base(UserEventType.TwitchSubscription, "SubscriptionEvent")
        {
            User = user;
            DisplayName = displayName;
        }
    }
}