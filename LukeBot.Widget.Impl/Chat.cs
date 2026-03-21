using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.Services;
using System.Security.Cryptography;


namespace LukeBot.Widget.Impl
{
    /**
     * Widget used to display messages from Twitch Chat.
     *
     * Reacts to following events:
     *  - TwitchChatMessage - new message sent on Twitch chat
     *  - TwitchChatClearUser - request to remove messages from selected user
     *  - TwitchChatClearMessage - request to remove a specific message
     */
    public class Chat: IWidget
    {
        private void OnEvent<T>(object o, EventArgsBase args)
            where T: SerializableEventArgsBase
        {
            SendToWS((T)args);
        }

        protected override void OnConnected()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Subscribe(OnEvent<TwitchChatMessageArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Subscribe(OnEvent<TwitchChatUserClearArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Subscribe(OnEvent<TwitchChatMessageClearArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_WATCH_STREAK).Subscribe(OnEvent<TwitchWatchStreakArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHEER).Subscribe(OnEvent<TwitchCheerArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_SUBSCRIPTION).Subscribe(OnEvent<TwitchSubscriptionArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Subscribe(OnEvent<TwitchChannelPointsRedemptionArgs>);

            ServiceUtils.GetTwitchUserModule(mLBUser).RefreshEmotes();
        }

        protected override void OnDisconnected()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Unsubscribe(OnEvent<TwitchChatMessageArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Unsubscribe(OnEvent<TwitchChatUserClearArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Unsubscribe(OnEvent<TwitchChatMessageClearArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_WATCH_STREAK).Unsubscribe(OnEvent<TwitchWatchStreakArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHEER).Unsubscribe(OnEvent<TwitchCheerArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_SUBSCRIPTION).Unsubscribe(OnEvent<TwitchSubscriptionArgs>);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Unsubscribe(OnEvent<TwitchChannelPointsRedemptionArgs>);
        }

        protected override void OnLoad()
        {
        }

        protected override void OnUnload()
        {
        }

        protected override ConfigurationBase CreateDefaultConfiguration()
        {
            return new EmptyWidgetConfiguration();
        }

        public Chat(string lbUser, string id, string name)
            : base(lbUser, "Widgets/Chat.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.chat;
        }

        ~Chat()
        {
        }
    }
}
