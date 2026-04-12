using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Communication;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.User;
using LukeBot.API;
using Microsoft.VisualBasic;


namespace LukeBot.Twitch.Impl
{
    internal class IRCChannel: IEventPublisher, IDisposable
    {
        private const string TAG_MOD = "mod";
        private const string TAG_VIP = "vip";
        private const string TAG_SUBSCRIBER = "subscriber";
        private const string TAG_ID = "id";
        private const string TAG_USER_ID = "user-id";
        private const string TAG_DISPLAY_NAME = "display-name";
        private const string TAG_COLOR = "color";
        private const string TAG_BADGES = "badges";
        private const string TAG_EMOTES = "emotes";
        private const string TAG_MSG_ID = "msg-id";
        private const string TAG_MSG_PARAM_ID = "msg-param-id";
        private const string TAG_MSG_PARAM_CATEGORY = "msg-param-category";
        private const string TAG_MSG_VALUE = "msg-param-value";

        private const string MSG_ID_VIEWER_MILESTONE = "viewermilestone";
        private const string MSG_CATEGORY_WATCH_STREAK = "watch-streak";

        private IUserContext mLBUser;
        private Token mChannelToken;
        private string mChannelName;
        private TwitchUserIdentity mChannelIdentity;
        private Dictionary<string, ICommand> mCommands = new();
        private int mMsgIDCounter = 0; // backup for when we don't have metadata
        private EventCallback mMessageEventCallback;
        private EventCallback mMessageClearEventCallback;
        private EventCallback mUserClearEventCallback;
        private EventCallback mWatchStreakEventCallback;

        private int messageCounter = 0;
        private int noticeCounter = 0;

        private TwitchUserModule GetUserModule()
        {
            return Service.Get<ITwitchService>().GetModule(mLBUser) as TwitchUserModule;
        }

        private string GetBackupMessageID()
        {
            return String.Format("message-{0}", messageCounter++);
        }

        private string GetBackupNoticeID()
        {
            return String.Format("notice-{0}", noticeCounter++);
        }

        private ChatUser EstablishUserIdentity(IRCMessage m, bool tagsEnabled)
        {
            ChatUser identity = ChatUser.Chatter;

            if (m.User == m.Channel)
                identity |= ChatUser.Broadcaster;

            if (tagsEnabled)
            {
                string isMod;
                if (m.GetTag(TAG_MOD, out isMod) && Int32.Parse(isMod) == 1)
                    identity |= ChatUser.Moderator;

                string isVIP;
                if (m.GetTag(TAG_VIP, out isVIP) && Int32.Parse(isVIP) == 1)
                    identity |= ChatUser.VIP;

                string isSub;
                if (m.GetTag(TAG_SUBSCRIBER, out isSub) && Int32.Parse(isSub) == 1)
                    identity |= ChatUser.Subscriber;
            }

            return identity;
        }

        public string ProcessMessageCommand(string cmd, ChatUser userIdentity, string[] args)
        {
            if (!mCommands.ContainsKey(cmd))
            {
                // TODO activate below with a launch argument or a property
                //return String.Format("Unrecognized command: {0}", cmd);
                return "";
            }

            Logger.Log().Debug("Processing command {0}", cmd);
            ICommand c = mCommands[cmd];

            if (!c.IsEnabled())
            {
                Logger.Log().Debug("Command {0} is disabled", cmd);
                return "";
            }

            if (!c.CheckPrivilege(userIdentity))
            {
                Logger.Log().Debug("Privilege check denied for command {0}", cmd);
                return "";
            }

            return mCommands[cmd].Execute(userIdentity, args);
        }

        // IEventPublisher implementations

        private EventArgsBase GenerateTestWatchStreakEvent(IEnumerable<(string attrib, string value)> args)
        {
            string user = "test_user";
            string displayName = "Test_User";
            int streak = 10;
            string message = "";

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "User": user = a.v; break;
                case "DisplayName": displayName = a.v; break;
                case "Streak": streak = Int32.Parse(a.v); break;
                case "Message": message = a.v; break;
                default:
                    Logger.Log().Warning("Unknown test event arg: {0}", a.a);
                    break;
                }
            }

            TwitchWatchStreakArgs ret = new("notice-test", user, displayName, streak);

            if (message.Length > 0)
            {
                // NOTE: This does NOT support Twitch subscriber emotes, only external ones
                // but I figured it's just a test message, so we don't need those anyway
                TwitchChatMessageArgs msg = new(Guid.NewGuid().ToString(), user, displayName, message);
                msg.Color = "#5060dd";
                msg.AddBadges(GetUserModule().GetBadges("broadcaster/1,vip/1"));
                AddExternalEmotesToMessage(msg);

                ret.AddMessage(msg);
            }

            return ret;
        }

        public string GetEventPublisherName()
        {
            return "TwitchIRC";
        }

        public List<EventDescriptor> GetEvents()
        {
            List<EventDescriptor> events = new();

            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHAT_MESSAGE,
                Description = "Twitch Chat message event. Emitted when any Twitch user sends a chat message.",
                Dispatcher = null
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHAT_CLEAR_MESSAGE,
                Description = "Twitch Chat Clear Message event. Emitted when a chat message is removed from the chat window.",
                Dispatcher = null
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHAT_CLEAR_USER,
                Description = "Twitch Chat Clear User event. Emitted when user's messages are removed from chat window (ie. because user is timed out).",
                Dispatcher = null
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_WATCH_STREAK,
                Description = "Twitch Watch Streak event. Emitted when user shares a Watch Streak celebration message",
                Dispatcher = Twitch.Utils.DispatcherNameForUser(mLBUser),
                TestGenerator = GenerateTestWatchStreakEvent,
                TestParams = new List<EventTestParam>()
                {
                    new() { Name = "User", Description = "Username of watch streak sharer", Type = EventTestParamType.String },
                    new() { Name = "DisplayName", Description = "Display name of watch streak sharer", Type = EventTestParamType.String },
                    new() { Name = "Streak", Description = "Watch streak count", Type = EventTestParamType.Integer },
                    new() { Name = "Message", Description = "Message added to watch streak share", Type = EventTestParamType.String }
                }
            });

            return events;
        }


        // IDisposable

        public void Dispose()
        {
            ServiceUtils.GetEventService().User(mLBUser.GetUsername()).UnregisterPublisher(this);
        }


        // Public methods

        public IRCChannel(IUserContext lbUser, TwitchUserIdentity channelIdentity, Token userToken)
        {
            mLBUser = lbUser;
            mChannelToken = userToken;
            mChannelName = channelIdentity.Username;
            mChannelIdentity = channelIdentity;

            List<EventCallback> events = ServiceUtils.GetEventService().User(mLBUser.GetUsername()).RegisterPublisher(this);

            foreach (EventCallback e in events)
            {
                switch (e.eventName)
                {
                case Events.TWITCH_CHAT_MESSAGE:
                    mMessageEventCallback = e;
                    break;
                case Events.TWITCH_CHAT_CLEAR_MESSAGE:
                    mMessageClearEventCallback = e;
                    break;
                case Events.TWITCH_CHAT_CLEAR_USER:
                    mUserClearEventCallback = e;
                    break;
                case Events.TWITCH_WATCH_STREAK:
                    mWatchStreakEventCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }
        }

        private TwitchChatMessageArgs FormChatMessageEvent(IRCMessage m, bool tagsEnabled)
        {
            string chatMsg = m.GetTrailingParam();

            // Message related tags pulled from metadata (if available)
            string msgID;
            if (!tagsEnabled || !m.GetTag(TAG_ID, out msgID))
                msgID = String.Format("{0}", mMsgIDCounter++);

            string displayName;
            if (!tagsEnabled || !m.GetTag(TAG_DISPLAY_NAME, out displayName))
                displayName = m.User;

            TwitchChatMessageArgs message = new TwitchChatMessageArgs(msgID, m.User, displayName, chatMsg);

            if (tagsEnabled)
            {
                string userID;
                if (m.GetTag(TAG_USER_ID, out userID))
                    message.UserID = userID;

                string color;
                if (m.GetTag(TAG_COLOR, out color))
                    message.Color = color;

                // Twitch global/sub emotes - taken from IRC tags
                string emotes;
                if (m.GetTag(TAG_EMOTES, out emotes))
                {
                    message.ParseEmotesString(chatMsg, emotes);
                }

                if (m.GetTag(TAG_BADGES, out string badges) && badges != null && badges.Length > 0)
                {
                    message.AddBadges(GetUserModule().GetBadges(badges));
                }
            }
            else
            {
                message.UserID = m.User;
                message.DisplayName = m.User;
            }

            AddExternalEmotesToMessage(message);

            return message;
        }

        public string ProcessMSG(IRCMessage m, bool tagsEnabled)
        {
            TwitchChatMessageArgs message = FormChatMessageEvent(m, tagsEnabled);

            mMessageEventCallback.PublishEvent(message);

            // update TwitchUserCollection
            try
            {
                TwitchUserCollection collection = GetUserModule().GetTwitchUsers();
                TwitchUserIdentity identity = collection.FetchUser(mChannelToken, true, message.User);
                identity.Badges = message.Badges;
            }
            catch (System.Exception e) when (e is KeyNotFoundException || e is APIErrorException)
            {
                // this should not happen techincally, but it either means the user was not found
                // in the collection, or it does not exist on Twitch. Log a warning anyway in case
                // this actually happens and continue on.
                Logger.Log().Warning("IRCChannel #{0}: User {1} seems to not exist, cannot update Identity.", mChannelName, message.User);
            }

            // Command processing
            string chatMsg = m.GetTrailingParam();
            string[] chatMsgTokens = chatMsg.Split(' ');
            string cmd = chatMsgTokens[0];
            ChatUser userIdentity = EstablishUserIdentity(m, tagsEnabled);

            string response = ProcessMessageCommand(cmd, userIdentity, chatMsgTokens);

            // TODO post LukeBot's response as an Event if desired
            //  - Has to re-do this path - the smartest would be to re-call this method
            //  - Also check the config if this is a wanted behavior
            //if (response.Length > 0)
            //    ...

            return response;
        }

        public void ProcessCLEARCHAT(string nick)
        {
            TwitchChatUserClearArgs message = new TwitchChatUserClearArgs(nick);
            mUserClearEventCallback.PublishEvent(message);
        }

        public void ProcessCLEARMSG(string msg, string msgID)
        {
            TwitchChatMessageClearArgs message = new TwitchChatMessageClearArgs(msg);
            message.MessageID = msgID;
            mMessageClearEventCallback.PublishEvent(message);
        }

        private void ProcessWatchStreak(IRCMessage m, int streak)
        {
            // Assumes tags are enabled up to this point
            // otherwise ProcessUSERNOTICE won't get to this point
            string noticeID;
            if (m.GetTag(TAG_ID, out string noticeIDFetched))
                noticeID = noticeIDFetched;
            else
                noticeID = GetBackupNoticeID();

            string displayName = m.User;
            if (m.GetTag(TAG_DISPLAY_NAME, out string displayNameFetched))
                displayName = displayNameFetched;

            TwitchWatchStreakArgs wsArgs = new(noticeID, m.User, displayName, streak);

            string chatMsg = m.GetTrailingParam();
            if (chatMsg.Length > 0)
            {
                // form and add TwitchChatMessage to the Watch Streak notice
                wsArgs.AddMessage(FormChatMessageEvent(m, true));
            }

            mWatchStreakEventCallback.PublishEvent(wsArgs);
        }

        public void ProcessUSERNOTICE(IRCMessage m, bool tagsEnabled)
        {
            if (!tagsEnabled)
                return; // can't process anything without tags

            if (m.GetTag(TAG_MSG_ID, out string msgID) &&
                m.GetTag(TAG_MSG_PARAM_CATEGORY, out string msgCategory))
            {
                if (msgID != MSG_ID_VIEWER_MILESTONE)
                    return; // only processing viewer milestones for now

                switch (msgCategory)
                {
                case MSG_CATEGORY_WATCH_STREAK:
                {
                    m.GetTag(TAG_MSG_VALUE, out string msgValue);
                    ProcessWatchStreak(m, Int32.Parse(msgValue));
                    break;
                }
                }
            }
        }

        public void AddCommand(string name, ICommand command)
        {
            if (mCommands.ContainsKey(name))
                throw new ArgumentException(String.Format("Command {0} already exists for channel {1}", name, mChannelName));

            mCommands.Add(name, command);
        }

        public void DeleteCommand(string name)
        {
            if (!mCommands.ContainsKey(name))
                throw new ArgumentException(String.Format("Command {0} does not exist for channel {1}", name, mChannelName));

            mCommands.Remove(name);
        }

        public void EditCommand(string name, string newValue)
        {
            ICommand cmd;
            if (!mCommands.TryGetValue(name, out cmd))
                throw new ArgumentException(String.Format("Command {0} does not exist for channel {1}", name, mChannelName));

            cmd.Edit(newValue);
        }

        public void AddExternalEmotesToMessage(TwitchChatMessageArgs message)
        {
            message.AddExternalEmotes(GetUserModule().ParseEmotes(message.Message));
        }

        public Dictionary<string, ICommand> GetCommands()
        {
            return mCommands;
        }

        public ICommand GetCommand(string name)
        {
            return mCommands[name];
        }

        public List<Descriptor> GetCommandDescriptors()
        {
            List<Descriptor> cmdDescs = new List<Descriptor>();

            Dictionary<string, ICommand> cmds = GetCommands();
            foreach (ICommand cmd in cmds.Values)
                cmdDescs.Add(cmd.ToDescriptor());

            return cmdDescs;
        }

        public Descriptor GetCommandDescriptor(string name)
        {
            return GetCommand(name).ToDescriptor();
        }

        public string GetChannelName()
        {
            return mChannelName;
        }
    };
}
