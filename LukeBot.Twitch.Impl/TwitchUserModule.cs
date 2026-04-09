using System;
using System.Collections.Generic;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch;
using LukeBot.Twitch.Command;
using LukeBot.User;
using Widget = LukeBot.Widget;

using CommonConstants = LukeBot.Common.Constants;
using LukeBot.Communication;

namespace LukeBot.Twitch.Impl
{
    public class TwitchUserModule: ITwitchUserModule
    {
        private IUserContext mLBUser;
        private TwitchIRC mIRC;
        private IRCChannel mIRCChannel;
        private Token mUserToken;
        private TwitchUserIdentity mChannelIdentity;
        private object mImplLock = new();
        private EventSubClient mEventSub;
        private TwitchUserCollection mTwitchUsers;
        private EmoteProvider mExternalEmotes;
        private BadgeCollection mChannelBadges;
        private readonly List<string> mEventSubEvents = new List<string>
        {
            EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD,
            EventSubClient.SUB_SUBSCRIPTION_GIFT,
            EventSubClient.SUB_SUBSCRIPTION_MESSAGE
        };


        private Path GetCommandCollectionPropertyName()
        {
            return Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(mLBUser.GetUsername())
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(Constants.PROP_TWITCH_COMMANDS);
        }

        private string GetTwitchChannel()
        {
            return Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(mLBUser.GetUsername())
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );
        }

        private void UpdateCommandInConfig(string commandName)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();

            Descriptor[] commands = Conf.Get<Descriptor[]>(cmdCollectionProp);

            int idx = Array.FindIndex<Descriptor>(commands, (Descriptor d) => d.Name == commandName);
            commands[idx] = GetChatCommandDescriptor(commandName);
            Conf.Modify<Descriptor[]>(cmdCollectionProp, commands);
        }

        private void LoadCommandsFromConfig()
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();

            Descriptor[] commands;
            if (!Conf.TryGet<Descriptor[]>(cmdCollectionProp, out commands))
                return; // quiet exit, assume user does not have any commands for Twitch chat

            string twitchChannel = GetTwitchChannel();
            foreach (Descriptor cmd in commands)
            {
                mIRCChannel.AddCommand(cmd.Name, AllocateChatCommand(cmd));
            }
        }

        private void SaveCommandToConfig(string name, ICommand cmd)
        {
            Descriptor desc = cmd.ToDescriptor();

            Path cmdCollectionProp = GetCommandCollectionPropertyName();
            ConfUtil.ArrayAppend(cmdCollectionProp, desc, new DescriptorComparer());
        }

        private void RemoveCommandFromConfig(string name)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();
            ConfUtil.ArrayRemove<Descriptor>(cmdCollectionProp, (Descriptor d) => d.Name != name);
        }

        private ICommand AllocateChatCommand(Descriptor d)
        {
            ICommand cmd = null;

            switch (d.Type)
            {
            case CommandType.print: cmd = new Command.Print(d); break;
            case CommandType.shoutout: cmd = new Command.Shoutout(d); break;
            case CommandType.addcom: cmd = new Command.AddCommand(d, mLBUser); break;
            case CommandType.editcom: cmd = new Command.EditCommand(d, mLBUser); break;
            case CommandType.delcom: cmd = new Command.DeleteCommand(d, mLBUser); break;
            case CommandType.counter: cmd = new Command.Counter(d); break;
            case CommandType.songrequest: cmd = new Command.SongRequest(d, mLBUser); break;
            default: return null;
            }

            cmd.SetUpdateConfigDelegate((string name) => UpdateCommandInConfig(name));
            return cmd;
        }


        internal TwitchUserModule(IUserContext lbUser, Token botToken, TwitchIRC IRC, BadgeCollection globalBadges)
        {
            mLBUser = lbUser;
            mTwitchUsers = new();
            string channelName = GetTwitchChannel();

            // fetch joined channel as a Twitch identity
            mChannelIdentity = mTwitchUsers.FetchUser(botToken, false, channelName);

            // TODO token's scope should be moved to Config
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, channelName);
            mUserToken.SetScope(new List<string>
            {
                "user:read:email",
                "channel:read:redemptions",
                "channel:read:subscriptions"
            });

            if (!mUserToken.Loaded)
                mUserToken.Request();

            mUserToken.EnsureValid();

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            // Each user has its own subscriber-queued dispatcher to independently handle some events
            ServiceUtils.GetEventService().User(mLBUser.GetUsername()).AddEventDispatcher(Twitch.Utils.DispatcherNameForUser(mLBUser), EventDispatcherType.SubscriberQueued);

            // initialize Twitch service clients
            mIRC = IRC;
            mIRCChannel = mIRC.JoinChannel(mLBUser, mChannelIdentity, mUserToken);
            mEventSub = new(mLBUser, mChannelIdentity);

            // preload necessary information
            mChannelBadges = new(globalBadges);
            mChannelBadges.AddBadges(Utils.FetchBadges(mUserToken, mChannelIdentity.ID));
            mExternalEmotes = new();
            mExternalEmotes.AddEmoteSource(new FFZEmoteSource(mChannelIdentity.ID));
            mExternalEmotes.AddEmoteSource(new BTTVEmoteSource(mChannelIdentity.ID));
            mExternalEmotes.AddEmoteSource(new SevenTVEmoteSource(mChannelIdentity.ID));

            try
            {
                // fetch users currently in the chat room and their information
                mTwitchUsers.FetchChatters(mUserToken, mChannelIdentity.ID);
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to fetch chatters, caught {0} - {1}", e.GetType().ToString(), e.Message);
                Logger.Log().Warning("Chatters list might be incomplete when requested - will be filled only based on active chatters.");
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        internal TwitchUserIdentity GetChannelIdentity()
        {
            return mChannelIdentity;
        }

        internal Token GetUserToken()
        {
            return mUserToken;
        }

        internal IUserContext GetLBUser()
        {
            return mLBUser;
        }

        internal string GetChannelName()
        {
            return mIRCChannel.GetChannelName();
        }

        internal List<MessageBadge> GetBadges(string badgeTag)
        {
            return mChannelBadges.GetBadges(badgeTag);
        }

        internal List<MessageEmote> ParseEmotes(string message)
        {
            return mExternalEmotes.ParseEmotes(message);
        }

        // ITwitchUserModule overrides //

        public void AddChatCommand(Descriptor d)
        {
            lock (mImplLock)
            {
                ICommand cmd = AllocateChatCommand(d);
                mIRCChannel.AddCommand(d.Name, cmd);
                SaveCommandToConfig(d.Name, cmd);
            }
        }

        public void AddChatCommand(string commandName, CommandType type, string value)
        {
            lock (mImplLock)
            {
                AddChatCommand(new Descriptor(commandName, type, value));
            }
        }

        public void DeleteChatCommand(string commandName)
        {
            lock (mImplLock)
            {
                mIRCChannel.DeleteCommand(commandName);
                RemoveCommandFromConfig(commandName);
            }
        }

        public void EditChatCommand(string commandName, string newValue)
        {
            lock (mImplLock)
            {
                mIRCChannel.EditCommand(commandName, newValue);
                UpdateCommandInConfig(commandName);
            }
        }

        public List<Descriptor> GetChatCommandDescriptors()
        {
            lock (mImplLock)
            {
                return mIRCChannel.GetCommandDescriptors();
            }
        }

        public Descriptor GetChatCommandDescriptor(string commandName)
        {
            lock (mImplLock)
            {
                return mIRCChannel.GetCommandDescriptor(commandName);
            }
        }

        public void AllowChatCommandPrivilege(string commandName, ChatUser privilege)
        {
            lock (mImplLock)
            {
                mIRCChannel.GetCommand(commandName).AllowUsers(privilege);
            }
        }

        public void DenyChatCommandPrivilege(string commandName, ChatUser privilege)
        {
            lock (mImplLock)
            {
                mIRCChannel.GetCommand(commandName).DenyUsers(privilege);
            }
        }

        public void SetChatCommandEnabled(string commandName, bool enabled)
        {
            lock (mImplLock)
            {
                mIRCChannel.GetCommand(commandName).SetEnabled(enabled);
            }
        }

        public void RefreshEmotes()
        {
            lock (mImplLock)
            {
                mExternalEmotes.Refresh();
            }
        }

        public void RestartEventSub()
        {
            lock (mImplLock)
            {
                mEventSub.RequestShutdown();
                mEventSub.WaitForShutdown();

                mEventSub = new(mLBUser, mChannelIdentity);
                mEventSub.Connect(mUserToken);
                mEventSub.Subscribe(mEventSubEvents);
            }
        }

        public void UpdateLogin(string newLogin)
        {
            // TODO:
            // - Part from current channel
            // - Add a new channel
            throw new NotImplementedException("Updating login for Twitch modules not yet implemented");
        }

        public IEnumerable<string> GetKnownChatUsers()
        {
            return mTwitchUsers.KnownUsers();
        }

        public Chatter GetChatter(string username)
        {
            return mTwitchUsers.Username(username).ToChatter();
        }


        // IUserModule overrides //

        public void Run()
        {
            try
            {
                LoadCommandsFromConfig();

                mEventSub.Connect(mUserToken);
                mEventSub.Subscribe(mEventSubEvents);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to subscribe to EventSub for user {0}: {1}",
                    mLBUser.GetUsername(), e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        public void RequestShutdown()
        {
            if (mEventSub != null) mEventSub.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mEventSub != null)
            {
                mEventSub.WaitForShutdown();
                mEventSub = null;
            }

            if (mIRCChannel != null)
            {
                mIRC.PartChannel(mIRCChannel);

                mIRCChannel = null;
                mIRC = null;
            }

            ServiceUtils.GetEventService().User(mLBUser.GetUsername()).RemoveEventDispatcher(Twitch.Utils.DispatcherNameForUser(mLBUser));
        }

        public string GetModuleType()
        {
            return CommonConstants.TWITCH_SERVICE_NAME;
        }

        public void Dispose()
        {
            RequestShutdown();
            WaitForShutdown();
        }
    }
}
