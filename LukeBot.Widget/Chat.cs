using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class Chat: IWidget
    {
        private void OnMessage(object o, EventArgsBase args)
        {
            TwitchChatMessageArgs a = (TwitchChatMessageArgs)args;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        private void OnClearChat(object o, EventArgsBase args)
        {
            TwitchChatUserClearArgs a = (TwitchChatUserClearArgs)args;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        private void OnClearMsg(object o, EventArgsBase args)
        {
            TwitchChatMessageClearArgs a = (TwitchChatMessageClearArgs)args;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        public Chat(string lbUser, string id, string name)
            : base("LukeBot.Widget/Widgets/Chat.html", id, name)
        {
            Comms.Event.User(lbUser).Event(Events.TWITCH_CHAT_MESSAGE).Endpoint += OnMessage;
            Comms.Event.User(lbUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Endpoint += OnClearChat;
            Comms.Event.User(lbUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Endpoint += OnClearMsg;
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
