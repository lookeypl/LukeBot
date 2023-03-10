using System;
using System.Net;
using System.Collections.Generic;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Twitch.Common;
using CommonUtils = LukeBot.Common.Utils;


namespace LukeBot.Twitch
{
    public class TwitchMainModule
    {
        private string mBotLogin;
        private Token mToken;
        private TwitchIRC mIRC;
        private API.Twitch.GetUserResponse mBotData;
        private Dictionary<string, TwitchUserModule> mUsers;


        private string GetCommandCollectionPropertyName(string lbUser)
        {
            return CommonUtils.FormConfName(
                LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN,
                lbUser,
                Constants.SERVICE_NAME,
                Constants.PROP_TWITCH_COMMANDS
            );
        }

        private string GetTwitchChannel(string lbUser)
        {
            return Conf.Get<string>(CommonUtils.FormConfName(
                LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN,
                lbUser,
                Constants.SERVICE_NAME,
                Constants.PROP_TWITCH_USER_LOGIN
            ));
        }

        private void EditCommandInConfig(string lbUser, string commandName, string newValue)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command.Descriptor[] commands = Conf.Get<Command.Descriptor[]>(cmdCollectionProp);

            int idx = Array.FindIndex<Command.Descriptor>(commands, (Command.Descriptor d) => d.Name == commandName);
            commands[idx].UpdateValue(newValue);
            Conf.Modify<Command.Descriptor[]>(cmdCollectionProp, commands);
        }

        private void LoadCommandsFromConfig(string lbUser)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command.Descriptor[] commands;
            if (!Conf.TryGet<Command.Descriptor[]>(cmdCollectionProp, out commands))
                return; // quiet exit, assume user does not have any commands for Twitch chat

            foreach (Command.Descriptor cmd in commands)
            {
                AddCommandToChannel(lbUser, cmd.Name, AllocateCommand(cmd));
            }
        }

        private void SaveCommandToConfig(string lbUser, string name, Command.ICommand cmd)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command.Descriptor desc = cmd.ToDescriptor();

            Command.Descriptor[] commands;
            if (!Conf.TryGet<Command.Descriptor[]>(cmdCollectionProp, out commands))
            {
                commands = new Command.Descriptor[1];
                commands[0] = desc;
                Conf.Add(cmdCollectionProp, Property.Create<Command.Descriptor[]>(commands));
                return;
            }

            Array.Resize(ref commands, commands.Length + 1);
            commands[commands.Length - 1] = desc;
            Array.Sort<Command.Descriptor>(commands, new Command.DescriptorComparer());
            Conf.Modify<Command.Descriptor[]>(cmdCollectionProp, commands);
        }

        private void RemoveCommandFromConfig(string lbUser, string name)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command.Descriptor[] commands;
            if (!Conf.TryGet<Command.Descriptor[]>(cmdCollectionProp, out commands))
            {
                return;
            }

            commands = Array.FindAll<Command.Descriptor>(commands, (Command.Descriptor d) => d.Name != name);
            Conf.Modify<Command.Descriptor[]>(cmdCollectionProp, commands);
        }

        public TwitchMainModule()
        {
            Comms.Communication.Register(Constants.SERVICE_NAME);

            mBotLogin = Conf.Get<string>("twitch.login");
            if (mBotLogin == LukeBot.Common.Constants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }

            string tokenScope = "chat:read chat:edit user:read:email"; // TODO should also be from Config...
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.Twitch.GetUser(mToken);
            mIRC = new TwitchIRC(mBotLogin, mToken);
            mUsers = new Dictionary<string, TwitchUserModule>();
        }

        public TwitchUserModule JoinChannel(string lbUser)
        {
            string channel = Conf.Get<string>(
                CommonUtils.FormConfName(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN, lbUser, Constants.SERVICE_NAME, Constants.PROP_TWITCH_USER_LOGIN)
            );

            if (mUsers.ContainsKey(channel))
            {
                throw new ChannelAlreadyJoinedException("Cannot join channel {0} - already joined", channel);
            }

            Logger.Log().Debug("Joining channel {0}", channel);

            TwitchUserModule user = new TwitchUserModule(lbUser, mToken, channel);

            mIRC.JoinChannel(user.GetUserData());

            // TODO uncomment when PropertyStore can properly load arrays of non-wow
            //LoadCommandsFromConfig(lbUser);

            mUsers.Add(channel, user);

            Logger.Log().Secure("Joined channel twitch ID: {0}", user.GetUserData().data[0].id);
            return user;
        }

        public void AddCommandToChannel(string lbUser, string commandName, Command.ICommand command)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.AddCommandToChannel(twitchChannel, commandName, command);
            //SaveCommandToConfig(lbUser, commandName, command);
        }

        public Twitch.Command.ICommand AllocateCommand(Command.Descriptor d)
        {
            return AllocateCommand(d.Name, d.Type, d.Value);
        }

        public Twitch.Command.ICommand AllocateCommand(string name, TwitchCommandType type, string value)
        {
            switch (type)
            {
            case TwitchCommandType.print: return new Command.Print(name, value);
            case TwitchCommandType.shoutout: return new Command.Shoutout(name);
            default: return null;
            }
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void DeleteCommandFromChannel(string lbUser, string commandName)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.DeleteCommandFromChannel(twitchChannel, commandName);
            //RemoveCommandFromConfig(lbUser, commandName);
        }

        public void EditCommandFromChannel(string lbUser, string commandName, string newValue)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.EditCommandFromChannel(twitchChannel, commandName, newValue);
            //EditCommandInConfig(lbUser, commandName, newValue);
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.RequestShutdown();

            if (mIRC != null) mIRC.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.WaitForShutdown();

            if (mIRC != null) mIRC.WaitForShutdown();
        }
    }
}