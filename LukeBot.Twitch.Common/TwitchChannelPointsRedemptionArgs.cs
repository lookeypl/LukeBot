using LukeBot.Communication.Common;


namespace LukeBot.Twitch.Common
{
    public class TwitchChannelPointsRedemptionArgs: UserEventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public string Title { get; private set; }

        public TwitchChannelPointsRedemptionArgs(string user, string displayName, string title)
            : base(UserEventType.TwitchChannelPointsRedemption, "ChannelPointsEvent")
        {
            User = user;
            DisplayName = displayName;
            Title = title;
        }
    }
}