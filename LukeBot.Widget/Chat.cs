using LukeBot.Communication;
using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.Widget.Common;
using LukeBot.Services;
using LukeBot.User;
using System.Runtime.CompilerServices;


namespace LukeBot.Widget
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
            IUserService userService = Service.Get(LukeBot.Common.Constants.USER_SERVICE_NAME) as IUserService;
            IUserContext userContext = userService.GetUser(mLBUser);

            ITwitchService service = Service.Get(LukeBot.Common.Constants.TWITCH_SERVICE_NAME) as ITwitchService;
            ITwitchUserModule userModule = service.GetModule(userContext) as ITwitchUserModule;
            userModule.RefreshEmotes();
        }

        protected override void OnLoad()
        {
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Endpoint += OnMessage;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Endpoint += OnClearChat;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Endpoint += OnClearMsg;
        }

        protected override void OnUnload()
        {
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Endpoint -= OnMessage;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Endpoint -= OnClearChat;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Endpoint -= OnClearMsg;
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
