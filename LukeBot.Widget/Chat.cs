using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Events;


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

        public Chat()
            : base("LukeBot.Widget/Widgets/Chat.html")
        {
            Comms.Event.TwitchChatMessage += OnMessage;
            Comms.Event.TwitchChatUserClear += OnClearChat;
            Comms.Event.TwitchChatMessageClear += OnClearMsg;
        }

        ~Chat()
        {
        }
    }
}
