using System.Collections.Generic;
using System;
using LukeBot.Common;
using LukeBot.Communication.Events;
using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch
{
    class IRCChannel
    {
        private string mChannelName;
        private API.Twitch.GetUserData mUserData;
        private Dictionary<string, Command.ICommand> mCommands;
        private EmoteProvider mExternalEmotes;

        public IRCChannel(API.Twitch.GetUserData userData)
        {
            mChannelName = userData.login;
            mUserData = userData;
            mCommands = new Dictionary<string, Command.ICommand>();
            mExternalEmotes = new EmoteProvider();

            mExternalEmotes.AddEmoteSource(new FFZEmoteSource(userData.id));
            mExternalEmotes.AddEmoteSource(new BTTVEmoteSource(userData.id));
            mExternalEmotes.AddEmoteSource(new SevenTVEmoteSource(userData.id));
        }

        public string ProcessMessage(string cmd, Command::User userIdentity, string[] args)
        {
            if (!mCommands.ContainsKey(cmd))
            {
                // TODO activate below with a launch argument or a property
                //return String.Format("Unrecognized command: {0}", cmd);
                return "";
            }

            Logger.Log().Debug("Processing command {0}", cmd);
            Command.ICommand c = mCommands[cmd];

            if (!c.CheckPrivilege(userIdentity))
            {
                Logger.Log().Debug("Privilege check denied for command {0}", cmd);
                return "";
            }

            return mCommands[cmd].Execute(args);
        }

        public void AddCommand(string name, Command.ICommand command)
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
            Command.ICommand cmd;
            if (!mCommands.TryGetValue(name, out cmd))
                throw new ArgumentException(String.Format("Command {0} does not exist for channel {1}", name, mChannelName));

            cmd.Edit(newValue);
        }

        public void AddExternalEmotesToMessage(TwitchChatMessageArgs message)
        {
            message.AddExternalEmotes(mExternalEmotes.ParseEmotes(message.Message));
        }

        public Dictionary<string, Command.ICommand> GetCommands()
        {
            return mCommands;
        }

        public Command.ICommand GetCommand(string name)
        {
            return mCommands[name];
        }
    };
}
