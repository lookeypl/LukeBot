using System;
using System.Net;
using System.Collections.Generic;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Interface;
using LukeBot.Module;
using LukeBot.Twitch.Common;

using CommonUtils = LukeBot.Common.Utils;
using Command = LukeBot.Twitch.Common.Command;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Twitch
{
    public class TwitchMainModule: IMainModule
    {
        private string mBotLogin;
        private Token mToken;
        private TwitchIRC mIRC;
        private API.Twitch.GetUserResponse mBotData;
        private Dictionary<string, TwitchUserModule> mUsers;


        // Config interactions //

        private string GetCommandCollectionPropertyName(string lbUser)
        {
            return CommonUtils.FormConfName(
                LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN,
                lbUser,
                LukeBot.Common.Constants.TWITCH_MODULE_NAME,
                Constants.PROP_TWITCH_COMMANDS
            );
        }

        private string GetTwitchChannel(string lbUser)
        {
            return Conf.Get<string>(CommonUtils.FormConfName(
                LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN,
                lbUser,
                LukeBot.Common.Constants.TWITCH_MODULE_NAME,
                Constants.PROP_TWITCH_USER_LOGIN
            ));
        }

        private void UpdateCommandInConfig(string lbUser, string commandName)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor[] commands = Conf.Get<Command::Descriptor[]>(cmdCollectionProp);

            int idx = Array.FindIndex<Command::Descriptor>(commands, (Command::Descriptor d) => d.Name == commandName);
            commands[idx] = GetCommandDescriptor(lbUser, commandName);
            Conf.Modify<Command::Descriptor[]>(cmdCollectionProp, commands);
        }

        private void LoadCommandsFromConfig(string lbUser)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor[] commands;
            if (!Conf.TryGet<Command::Descriptor[]>(cmdCollectionProp, out commands))
                return; // quiet exit, assume user does not have any commands for Twitch chat

            string twitchChannel = GetTwitchChannel(lbUser);
            foreach (Command::Descriptor cmd in commands)
            {
                mIRC.AddCommandToChannel(twitchChannel, cmd.Name, AllocateCommand(lbUser, cmd));
            }
        }

        private void SaveCommandToConfig(string lbUser, string name, Command.ICommand cmd)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor desc = cmd.ToDescriptor();

            Command::Descriptor[] commands;
            if (!Conf.TryGet<Command::Descriptor[]>(cmdCollectionProp, out commands))
            {
                commands = new Command::Descriptor[1];
                commands[0] = desc;
                Conf.Add(cmdCollectionProp, Property.Create<Command::Descriptor[]>(commands));
                return;
            }

            Array.Resize(ref commands, commands.Length + 1);
            commands[commands.Length - 1] = desc;
            Array.Sort<Command::Descriptor>(commands, new Command::DescriptorComparer());
            Conf.Modify<Command::Descriptor[]>(cmdCollectionProp, commands);
        }

        private void RemoveCommandFromConfig(string lbUser, string name)
        {
            string cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor[] commands;
            if (!Conf.TryGet<Command::Descriptor[]>(cmdCollectionProp, out commands))
            {
                return;
            }

            commands = Array.FindAll<Command::Descriptor>(commands, (Command::Descriptor d) => d.Name != name);
            if (commands.Length == 0)
                Conf.Remove(cmdCollectionProp);
            else
                Conf.Modify<Command::Descriptor[]>(cmdCollectionProp, commands);
        }


        // Intercom interactions //

        private void IntercomAddCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            AddCommandIntercomMsg m = (AddCommandIntercomMsg)msg;

            try
            {
                AddCommandToChannel(m.User, m.Name, AllocateCommand(m.User, m.Name, m.Type, m.Param));
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }

        private void IntercomEditCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            EditCommandIntercomMsg m = (EditCommandIntercomMsg)msg;

            try
            {
                EditCommandFromChannel(m.User, m.Name, m.Param);
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }

        private void IntercomDeleteCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            DeleteCommandIntercomMsg m = (DeleteCommandIntercomMsg)msg;

            try
            {
                DeleteCommandFromChannel(m.User, m.Name);
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }


        // User Module Descriptor delegates //

        private bool UserModuleLoadPrerequisites(string lbUser)
        {
            string userTwitchLoginProp = LukeBot.Common.Utils.FormConfName(
                LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN, lbUser, LukeBot.Common.Constants.TWITCH_MODULE_NAME, Constants.PROP_TWITCH_USER_LOGIN
            );
            string login;
            if (Conf.TryGet<string>(userTwitchLoginProp, out login))
                return true; // quietly exit - login is already there, prerequisites are met

            // User login does not exist - query for it
            login = CLI.Instance.Query("Twitch login for user " + lbUser);
            if (login.Length == 0)
            {
                CLI.Instance.Message("No login provided");
                return false;
            }

            Conf.Add(userTwitchLoginProp, Property.Create<string>(login));
            return true;
        }

        private IUserModule UserModuleLoader(string lbUser)
        {
            return JoinChannel(lbUser);
        }

        private void UserModuleUnloader(IUserModule module)
        {
            PartChannel(module as TwitchUserModule);
        }


        // Public methods //

        public TwitchMainModule()
        {
            Comms.Communication.Register(LukeBot.Common.Constants.TWITCH_MODULE_NAME);

            mBotLogin = Conf.Get<string>("twitch.login");
            if (mBotLogin == LukeBot.Common.Constants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }

            string tokenScope = "chat:read chat:edit user:read:email"; // TODO should also be from Config...
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin, mBotLogin);

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

            Intercom::EndpointInfo epInfo = new Intercom::EndpointInfo(Endpoints.TWITCH_MAIN_MODULE);
            epInfo.AddMessage(Messages.ADD_COMMAND, IntercomAddCommandDelegate);
            epInfo.AddMessage(Messages.EDIT_COMMAND, IntercomEditCommandDelegate);
            epInfo.AddMessage(Messages.DELETE_COMMAND, IntercomDeleteCommandDelegate);
            Comms.Intercom.Register(epInfo);
        }

        public TwitchUserModule JoinChannel(string lbUser)
        {
            string channel = Conf.Get<string>(
                CommonUtils.FormConfName(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN, lbUser, LukeBot.Common.Constants.TWITCH_MODULE_NAME, Constants.PROP_TWITCH_USER_LOGIN)
            );

            if (mUsers.ContainsKey(channel))
            {
                throw new ChannelAlreadyJoinedException(channel);
            }

            Logger.Log().Debug("Joining channel {0}", channel);

            TwitchUserModule user = new TwitchUserModule(lbUser, mToken, channel);

            try
            {
                mIRC.JoinChannel(user.GetUserData());

                LoadCommandsFromConfig(lbUser);

                mUsers.Add(channel, user);
            }
            catch (System.Exception)
            {
                // get rid of our just created user module, as we might've already started something
                user.RequestShutdown();
                user.WaitForShutdown();
                user = null;
                throw;
            }

            Logger.Log().Secure("Joined channel twitch ID: {0}", user.GetUserData().id);
            return user;
        }

        public void PartChannel(TwitchUserModule module)
        {
            Logger.Log().Debug("Parting channel {0}", module.GetChannelName());

            mIRC.PartChannel(module.GetUserData());

            mUsers.Remove(module.GetChannelName());

            Logger.Log().Secure("Parted channel twitch ID: {0}", module.GetUserData().id);
        }

        public void AddCommandToChannel(string lbUser, string commandName, Command.ICommand command)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.AddCommandToChannel(twitchChannel, commandName, command);
            SaveCommandToConfig(lbUser, commandName, command);
        }

        public Twitch.Command.ICommand AllocateCommand(string lbUser, Command::Descriptor d)
        {
            switch (d.Type)
            {
            case Command::Type.print: return new Command.Print(d);
            case Command::Type.shoutout: return new Command.Shoutout(d);
            case Command::Type.addcom: return new Command.AddCommand(d, lbUser);
            case Command::Type.editcom: return new Command.EditCommand(d, lbUser);
            case Command::Type.delcom: return new Command.DeleteCommand(d, lbUser);
            default: return null;
            }
        }

        public Twitch.Command.ICommand AllocateCommand(string lbUser, string name, Command::Type type, string value)
        {
            return AllocateCommand(lbUser, new Command::Descriptor(name, type, value));
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void DeleteCommandFromChannel(string lbUser, string commandName)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.DeleteCommandFromChannel(twitchChannel, commandName);
            RemoveCommandFromConfig(lbUser, commandName);
        }

        public void EditCommandFromChannel(string lbUser, string commandName, string newValue)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.EditCommandFromChannel(twitchChannel, commandName, newValue);
            UpdateCommandInConfig(lbUser, commandName);
        }

        public List<Command::Descriptor> GetCommandDescriptors(string lbUser)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            return mIRC.GetCommandDescriptors(twitchChannel);
        }

        public Command::Descriptor GetCommandDescriptor(string lbUser, string name)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            return mIRC.GetCommandDescriptor(twitchChannel, name);
        }

        public void AllowPrivilegeInCommand(string lbUser, string name, Command::User privilege)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.AllowPrivilegeInCommand(twitchChannel, name, privilege);
            UpdateCommandInConfig(twitchChannel, name);
        }

        public void DenyPrivilegeInCommand(string lbUser, string name, Command::User privilege)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.DenyPrivilegeInCommand(twitchChannel, name, privilege);
            UpdateCommandInConfig(twitchChannel, name);
        }

        public void UpdateLoginForUser(string lbUser, string newLogin)
        {
            // TODO:
            // - Part from current channel
            // - Add a new channel
            throw new NotImplementedException("Updating login for Twitch modules not yet implemented");
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Twitch;
            umd.LoadPrerequisite = UserModuleLoadPrerequisites;
            umd.Loader = UserModuleLoader;
            umd.Unloader = UserModuleUnloader;
            return umd;
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