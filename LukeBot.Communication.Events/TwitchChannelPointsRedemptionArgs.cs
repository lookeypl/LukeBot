namespace LukeBot.Communication.Events
{
    public class TwitchChannelPointsRedemptionArgs: EventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public string Title { get; private set; }

        public TwitchChannelPointsRedemptionArgs(string user, string displayName, string title)
            : base(Events.Type.TwitchChannelPointsRedemption, "ChannelPointsEvent")
        {
            User = user;
            DisplayName = displayName;
            Title = title;
        }
    }
}