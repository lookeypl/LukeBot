using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.Services;


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
        private void OnMessage(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatMessageArgs)args);
        }

        private void OnClearChat(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatUserClearArgs)args);
        }

        private void OnClearMsg(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatMessageClearArgs)args);
        }

        protected override void OnConnected()
        {
            ServiceUtils.GetTwitchUserModule(mLBUser).RefreshEmotes();
        }

        protected override void OnLoad()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Subscribe(OnMessage);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Subscribe(OnClearChat);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Subscribe(OnClearMsg);
        }

        protected override void OnUnload()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Unsubscribe(OnMessage);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Unsubscribe(OnClearChat);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Unsubscribe(OnClearMsg);
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
