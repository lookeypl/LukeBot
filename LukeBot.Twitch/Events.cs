using System;
using System.Collections.Generic;
using System.Text.Json;
using LukeBot.Logging;
using LukeBot.Common;
using System.Text.Json.Serialization;


namespace LukeBot.Twitch
{
    public class Events
    {
        public const string TWITCH_CHAT_MESSAGE = "TwitchChatMessage";
        public const string TWITCH_CHAT_CLEAR_MESSAGE = "TwitchChatClearMessage";
        public const string TWITCH_CHAT_CLEAR_USER = "TwitchChatClearUser";

        public const string TWITCH_WATCH_STREAK = "TwitchWatchStreak";

        public const string TWITCH_SUBSCRIPTION = "TwitchSubscription";

        public const string TWITCH_CHANNEL_POINTS_REDEMPTION = "TwitchChannelPointsRedemption";

        public const string TWITCH_CHEER = "TwitchCheer";
    }


    // Chat

    public class TwitchChatMessageArgs: SerializableEventArgsBase
    {
        public string MessageID { get; private set; }
        public string UserID { get; set; }
        public string Color { get; set; }
        public List<MessageEmote> Emotes { get; private set; }
        public List<MessageBadge> Badges { get; private set; }
        public string User { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }

        public TwitchChatMessageArgs(string msgID, string user, string displayName, string message)
            : base(Events.TWITCH_CHAT_MESSAGE)
        {
            MessageID = msgID;
            Message = message;
            User = user;
            DisplayName = displayName;
            UserID = "";
            Color = "#aaaaaa";
            Emotes = new();
            Badges = new();
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

                // "animated" is false because animated Twitch sub emotes don't have static versions
                // so we just ignore that field and assume emote will be animated if it is
                Emotes.Add(new MessageEmote(EmoteSource.Twitch, name, e.Substring(0, separatorIdx), 32, 32, e.Substring(separatorIdx + 1), false));
            }
        }

        public void AddBadges(List<MessageBadge> badges)
        {
            Badges = badges;
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

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchChatMessageArgs>(this);
        }
    }

    public class TwitchChatMessageClearArgs: SerializableEventArgsBase
    {
        public string Message { get; private set; }
        public string MessageID { get; set; }

        public TwitchChatMessageClearArgs(string message)
            : base(Events.TWITCH_CHAT_CLEAR_MESSAGE)
        {
            Message = message;
            MessageID = "";
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchChatMessageClearArgs>(this);
        }
    }

    public class TwitchChatUserClearArgs: SerializableEventArgsBase
    {
        public string Nick { get; private set; }

        public TwitchChatUserClearArgs(string nick)
            : base(Events.TWITCH_CHAT_CLEAR_USER)
        {
            Nick = nick;
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchChatUserClearArgs>(this);
        }
    }


    // Base for notices used by Chat

    public abstract class TwitchNoticeArgs: SerializableEventArgsBase
    {
        public string NoticeID { get; protected set; }
        public string User { get; protected set; }
        public string DisplayName { get; protected set; }
        public TwitchChatMessageArgs Message { get; protected set; } // optional, can be null

        protected TwitchNoticeArgs(string eventName, string noticeID, string user, string displayName)
            : base(eventName)
        {
            NoticeID = noticeID;
            User = user;
            DisplayName = displayName;
        }

        public void AddMessage(TwitchChatMessageArgs m)
        {
            Message = m;
        }

        public void AddMessage(string msg)
        {
            Message = new TwitchChatMessageArgs(Guid.NewGuid().ToString(), User, DisplayName, msg);
        }
    }


    // Subscriptions

    public enum TwitchSubscriptionType
    {
        New = 0,
        Resub,
        Gift
    }

    public class TwitchSubscriptionDetails
    {
        public TwitchSubscriptionType Type { get; private set; }
        public int Tier { get; protected set; }

        public TwitchSubscriptionDetails()
            : this(1)
        {
        }

        public TwitchSubscriptionDetails(int tier)
            : this(TwitchSubscriptionType.New, tier)
        {
        }

        internal TwitchSubscriptionDetails(TwitchSubscriptionType type, int tier)
        {
            Type = type;
            Tier = tier;
        }

        public virtual void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                }
            }
        }

        public override string ToString()
        {
            return String.Format("{0}, tier {1}", Type, Tier);
        }
    }

    public class TwitchResubscriptionDetails: TwitchSubscriptionDetails
    {
        public int Cumulative { get; private set; } // total subscription month
        public int Streak { get; private set; } // 0 if not shared
        public int Duration { get; private set; } // length of resub (1, 2, 3 months etc.)

        public TwitchResubscriptionDetails()
            : this(1, 1, 1, 1)
        {
        }

        public TwitchResubscriptionDetails(int tier, int cumulative, int streak, int duration)
            : base(TwitchSubscriptionType.Resub, tier)
        {
            Cumulative = cumulative;
            Streak = streak;
            Duration = duration;
        }

        public override void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;
            int cumulative = 3;
            int streak = 3;
            int duration = 1;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                case "Cumulative": cumulative = Int32.Parse(a.v); break;
                case "Streak": streak = Int32.Parse(a.v); break;
                case "Duration": duration = Int32.Parse(a.v); break;
                }
            }

            Tier = tier;
            Cumulative = cumulative;
            Streak = streak;
            Duration = duration;
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0} month, {1} streak", Cumulative, Streak);
        }
    }

    public class TwitchGiftSubscriptionDetails: TwitchSubscriptionDetails
    {
        public int RecipentCount { get; private set; }

        public TwitchGiftSubscriptionDetails()
            : this(1, 1)
        {
        }

        public TwitchGiftSubscriptionDetails(int tier, int recipentCount)
            : base(TwitchSubscriptionType.Gift, tier)
        {
            RecipentCount = recipentCount;
        }

        public override void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;
            int recipents = 10;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                case "Recipents": recipents = Int32.Parse(a.v); break;
                }
            }

            Tier = tier;
            RecipentCount = recipents;
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0} gifts", RecipentCount);
        }
    }

    public class TwitchSubscriptionArgs: TwitchNoticeArgs
    {
        public TwitchSubscriptionDetails Details { get; private set; }

        public TwitchSubscriptionArgs(string noticeID, string user, string displayName, string message, TwitchSubscriptionDetails details)
            : base(Events.TWITCH_SUBSCRIPTION, noticeID, user, displayName)
        {
            Details = details;
            Message = null;
            if (message != null && message.Length > 0)
            {
                AddMessage(message);
            }
        }

        public override string Serialize()
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new TwitchSubscriptionArgsJsonConverter());
            return JsonSerializer.Serialize<TwitchSubscriptionArgs>(this, opts);
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(" ({0}, {1})", DisplayName, Details);
        }
    }


    // Channel Points

    public class TwitchChannelPointsRedemptionArgs: TwitchNoticeArgs
    {
        public string Title { get; private set; }
        public int Cost { get; private set; }
        public string Prompt { get; private set; }

        public TwitchChannelPointsRedemptionArgs(string user, string displayName,
                string id, string title, int cost, string prompt, string message)
            : base(Events.TWITCH_CHANNEL_POINTS_REDEMPTION, id, user, displayName)
        {
            Title = title;
            Cost = cost;
            Prompt = prompt;
            Message = null;

            if (message != null && message.Length > 0)
            {
                AddMessage(message);
            }
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchChannelPointsRedemptionArgs>(this);
        }
    }


    // Cheers

    public class TwitchCheerArgs: TwitchNoticeArgs
    {
        public int Amount { get; private set; }

        public TwitchCheerArgs(string noticeID, string user, string displayName, int amount, string message)
            : base(Events.TWITCH_CHEER, noticeID, user, displayName)
        {
            Amount = amount;
            Message = null;
            if (message != null && message.Length > 0)
            {
                AddMessage(message);
            }
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchCheerArgs>(this);
        }
    }


    // Watch Streaks

    public class TwitchWatchStreakArgs: TwitchNoticeArgs
    {
        public int Streak { get; private set; }

        public TwitchWatchStreakArgs(string noticeID, string user, string displayName, int streak)
            : base(Events.TWITCH_WATCH_STREAK, noticeID, user, displayName)
        {
            Streak = streak;
            Message = null;
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<TwitchWatchStreakArgs>(this);
        }
    }
}
