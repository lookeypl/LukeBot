using System;
using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.Twitch.Common;


namespace LukeBot.Communication.Events
{
    public class TwitchChatMessageArgs: UserEventArgsBase
    {
        public string MessageID { get; private set; }
        public string UserID { get; set; }
        public string Color { get; set; }
        public List<MessageEmote> Emotes { get; private set; }
        public string Nick { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }

        public TwitchChatMessageArgs(string msgID)
            : base(UserEventType.TwitchChatMessage, "TwitchIRCMessage")
        {
            MessageID = msgID;
            UserID = "";
            Color = "#dddddd";
            Emotes = new List<MessageEmote>();
            Nick = "";
            DisplayName = "";
            Message = "";
        }

        private string GetEmoteName(string msg, string range)
        {
            int dash = range.IndexOf('-');
            int from = Int32.Parse(range.Substring(0, dash));
            int count = Int32.Parse(range.Substring(dash + 1)) - from + 1;
            return msg.Substring(from, count);
        }

        public void ParseEmotesString(string msg, string emotesStr)
        {
            if (emotesStr.Length == 0)
                return;

            string[] emotes = emotesStr.Split('/');
            foreach (string e in emotes)
            {
                int separatorIdx = e.IndexOf(':');
                string ranges = e.Substring(separatorIdx + 1);
                int firstRangeIdx = ranges.IndexOf(',');
                string name;
                if (firstRangeIdx == -1)
                    name = GetEmoteName(msg, ranges);
                else
                    name = GetEmoteName(msg, ranges.Substring(0, firstRangeIdx));

                Emotes.Add(new MessageEmote(EmoteSource.Twitch, name, e.Substring(0, separatorIdx), 32, 32, e.Substring(separatorIdx + 1)));
            }
        }

        public void AddExternalEmotes(List<MessageEmote> emotes)
        {
            List<MessageEmote> filteredEmotes = new List<MessageEmote>(emotes.Count);
            foreach (MessageEmote e in emotes)
            {
                if (Emotes.Exists(x => x.Name == e.Name))
                {
                    Logger.Log().Debug("Removing external emote {0} from message, duplicated by sub emotes", e.Name);
                    continue;
                }

                filteredEmotes.Add(e);
            }

            Emotes.AddRange(filteredEmotes);
        }
    }

    public class TwitchChatMessageClearArgs: UserEventArgsBase
    {
        public string Message { get; private set; }
        public string MessageID { get; set; }

        public TwitchChatMessageClearArgs(string message)
            : base(UserEventType.TwitchChatMessageClear, "TwitchIRCClearMsg")
        {
            Message = message;
            MessageID = "";
        }
    }

    public class TwitchChatUserClearArgs: UserEventArgsBase
    {
        public string Nick { get; private set; }

        public TwitchChatUserClearArgs(string nick)
            : base(UserEventType.TwitchChatUserClear, "TwitchIRCClearChat")
        {
            Nick = nick;
        }
    }
}