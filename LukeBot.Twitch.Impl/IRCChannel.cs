using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Communication;
using LukeBot.Twitch;
using LukeBot.Twitch.Command;
using LukeBot.API;


namespace LukeBot.Twitch.Impl
{
    internal class IRCChannel: IEventPublisher, IDisposable
    {
        private string mLBUser;
        private string mChannelName;
        private API.Twitch.GetUserData mUserData;
        private Dictionary<string, ICommand> mCommands = new();
        private EmoteProvider mExternalEmotes = new();
        private BadgeCollection mChannelBadges;
        private int mMsgIDCounter = 0; // backup for when we don't have metadata
        private EventCallback mMessageEventCallback;
        private EventCallback mMessageClearEventCallback;
        private EventCallback mUserClearEventCallback;

        private ChatUser EstablishUserIdentity(IRCMessage m, bool tagsEnabled)
        {
            ChatUser identity = ChatUser.Chatter;

            if (m.User == m.Channel)
                identity |= ChatUser.Broadcaster;

            if (tagsEnabled)
            {
                string isMod;
                if (m.GetTag("mod", out isMod) && Int32.Parse(isMod) == 1)
                    identity |= ChatUser.Moderator;

                string isVIP;
                if (m.GetTag("vip", out isVIP) && Int32.Parse(isVIP) == 1)
                    identity |= ChatUser.VIP;

                string isSub;
                if (m.GetTag("subscriber", out isSub) && Int32.Parse(isSub) == 1)
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

            return events;
        }


        // IDisposable

        public void Dispose()
        {
            Comms.Event.User(mLBUser).UnregisterPublisher(this);
        }


        // Public methods

        public IRCChannel(string lbUser, API.Twitch.GetUserData userData, Token userToken, BadgeCollection globalBadges)
        {
            mLBUser = lbUser;
            mChannelName = userData.login;
            mUserData = userData;
            mChannelBadges = new(globalBadges);
            mChannelBadges.AddBadges(Utils.FetchBadges(userToken, userData.id));

            mExternalEmotes.AddEmoteSource(new FFZEmoteSource(userData.id));
            mExternalEmotes.AddEmoteSource(new BTTVEmoteSource(userData.id));
            mExternalEmotes.AddEmoteSource(new SevenTVEmoteSource(userData.id));

            List<EventCallback> events = Comms.Event.User(mLBUser).RegisterPublisher(this);

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
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }
        }

        public string ProcessMSG(IRCMessage m, bool tagsEnabled)
        {
            string chatMsg = m.GetTrailingParam();

            // Message related tags pulled from metadata (if available)
            string msgID;
            if (!tagsEnabled || !m.GetTag("id", out msgID))
                msgID = String.Format("{0}", mMsgIDCounter++);

            TwitchChatMessageArgs message = new TwitchChatMessageArgs(msgID);
            message.Nick = m.User;
            message.Message = chatMsg;

            if (tagsEnabled)
            {
                string userID;
                if (m.GetTag("user-id", out userID))
                    message.UserID = userID;

                string color;
                if (m.GetTag("color", out color))
                    message.Color = color;

                string displayName;
                if (m.GetTag("display-name", out displayName))
                    message.DisplayName = displayName;

                // Twitch global/sub emotes - taken from IRC tags
                string emotes;
                if (m.GetTag("emotes", out emotes))
                {
                    message.ParseEmotesString(chatMsg, emotes);
                }

                if (m.GetTag("badges", out string badges) && badges != null && badges.Length > 0)
                {
                    message.AddBadges(mChannelBadges.GetBadges(badges));
                }
            }
            else
            {
                message.UserID = m.User;
                message.DisplayName = m.User;
            }

            AddExternalEmotesToMessage(message);

            mMessageEventCallback.PublishEvent(message);

            string[] chatMsgTokens = chatMsg.Split(' ');
            string cmd = chatMsgTokens[0];
            ChatUser userIdentity = EstablishUserIdentity(m, tagsEnabled);

            string response = ProcessMessageCommand(cmd, userIdentity, chatMsgTokens);

            // TODO post LukeBot's response if desired
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
            message.AddExternalEmotes(mExternalEmotes.ParseEmotes(message.Message));
        }

        public void RefreshEmotes()
        {
            mExternalEmotes.Refresh();
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
