using System;
using System.Collections.Generic;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch.EventSub;
using LukeBot.Twitch.Common;
using Widget = LukeBot.Widget;

using CommonConstants = LukeBot.Common.Constants;
using Command = LukeBot.Twitch.Common.Command;
using LukeBot.Twitch.Common.Command;

namespace LukeBot.Twitch
{
    public class TwitchUserModule: ITwitchUserModule
    {
        private string mLBUser;
        private TwitchIRC mIRC;
        private IRCChannel mIRCChannel;
        private Token mUserToken;
        private API.Twitch.GetUserData mUserData;
        private object mImplLock = new();
        private EventSubClient mEventSub;
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
                .Push(mLBUser)
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(Constants.PROP_TWITCH_COMMANDS);
        }

        private string GetTwitchChannel()
        {
            return Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(mLBUser)
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );
        }

        private void UpdateCommandInConfig(string commandName)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();

            Command::Descriptor[] commands = Conf.Get<Command::Descriptor[]>(cmdCollectionProp);

            int idx = Array.FindIndex<Command::Descriptor>(commands, (Command::Descriptor d) => d.Name == commandName);
            commands[idx] = GetChatCommandDescriptor(commandName);
            Conf.Modify<Command::Descriptor[]>(cmdCollectionProp, commands);
        }

        private void LoadCommandsFromConfig()
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();

            Command::Descriptor[] commands;
            if (!Conf.TryGet<Command::Descriptor[]>(cmdCollectionProp, out commands))
                return; // quiet exit, assume user does not have any commands for Twitch chat

            string twitchChannel = GetTwitchChannel();
            foreach (Command::Descriptor cmd in commands)
            {
                mIRCChannel.AddCommand(cmd.Name, AllocateChatCommand(cmd));
            }
        }

        private void SaveCommandToConfig(string name, Command::ICommand cmd)
        {
            Command::Descriptor desc = cmd.ToDescriptor();

            Path cmdCollectionProp = GetCommandCollectionPropertyName();
            ConfUtil.ArrayAppend(cmdCollectionProp, desc, new Command::DescriptorComparer());
        }

        private void RemoveCommandFromConfig(string name)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName();
            ConfUtil.ArrayRemove<Command::Descriptor>(cmdCollectionProp, (Command::Descriptor d) => d.Name != name);
        }

        private ICommand AllocateChatCommand(Descriptor d)
        {
            Command::ICommand cmd = null;

            switch (d.Type)
            {
            case Command::Type.print: cmd = new Command.Print(d); break;
            case Command::Type.shoutout: cmd = new Command.Shoutout(d); break;
            case Command::Type.addcom: cmd = new Command.AddCommand(d, mLBUser); break;
            case Command::Type.editcom: cmd = new Command.EditCommand(d, mLBUser); break;
            case Command::Type.delcom: cmd = new Command.DeleteCommand(d, mLBUser); break;
            case Command::Type.counter: cmd = new Command.Counter(d); break;
            case Command::Type.songrequest: cmd = new Command.SongRequest(d, mLBUser); break;
            default: return null;
            }

            cmd.SetUpdateConfigDelegate((string name) => UpdateCommandInConfig(name));
            return cmd;
        }


        internal TwitchUserModule(string lbUser, Token botToken, TwitchIRC IRC)
        {
            mLBUser = lbUser;
            string channelName = GetTwitchChannel();

            API.Twitch.GetUserResponse resp = API.Twitch.GetUser(botToken, channelName);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Error("Failed to fetch user data from Twitch - received error code {0}", resp.code.ToString());
                throw new APIResponseErrorException(resp.code);
            }
            mUserData = resp.data[0];

            // TODO token's scope should be moved to Config
            string tokenScope = "user:read:email channel:read:redemptions channel:read:subscriptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, channelName);

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            mUserToken.EnsureValid();

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            // Each user has its own queued dispatcher to independently handle some events
            Comms.Event.User(mLBUser).AddEventDispatcher(Constants.QueuedDispatcherForUser(mLBUser), EventDispatcherType.Queued);

            mIRC = IRC;
            mIRCChannel = mIRC.JoinChannel(mLBUser, mUserData, mUserToken);
            mEventSub = new(mLBUser);
        }

        internal API.Twitch.GetUserData GetUserData()
        {
            return mUserData;
        }

        internal Token GetUserToken()
        {
            return mUserToken;
        }

        internal string GetLBUser()
        {
            return mLBUser;
        }

        internal string GetChannelName()
        {
            return mIRCChannel.GetChannelName();
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

        public void AddChatCommand(string commandName, Common.Command.Type type, string value)
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
                mIRCChannel.RefreshEmotes();
            }
        }

        public void RestartEventSub()
        {
            lock (mImplLock)
            {
                mEventSub.RequestShutdown();
                mEventSub.WaitForShutdown();

                mEventSub = new(mLBUser);
                mEventSub.Connect(mUserToken, mUserData.id);
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


        // IUserModule overrides //

        public void Run()
        {
            try
            {
                LoadCommandsFromConfig();

                mEventSub.Connect(mUserToken, mUserData.id);
                mEventSub.Subscribe(mEventSubEvents);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to subscribe to EventSub for user {0}: {1}",
                    mLBUser, e.Message);
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

            Comms.Event.User(mLBUser).RemoveEventDispatcher(Constants.QueuedDispatcherForUser(mLBUser));

            if (mIRCChannel != null)
            {
                mIRC.PartChannel(mIRCChannel);

                mIRCChannel = null;
                mIRC = null;
            }
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
