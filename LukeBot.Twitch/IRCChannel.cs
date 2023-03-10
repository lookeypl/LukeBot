using System.Collections.Generic;
using System;


namespace LukeBot.Twitch
{
    class IRCChannel
    {
        private string mChannelName;
        private Dictionary<string, Command.ICommand> mCommands;

        public IRCChannel(string name)
        {
            mChannelName = name;
            mCommands = new Dictionary<string, Command.ICommand>();
        }

        public string ProcessMessage(string cmd, string[] args)
        {
            if (!mCommands.ContainsKey(cmd))
            {
                // TODO activate below with a launch argument or a property
                //return String.Format("Unrecognized command: {0}", cmd);
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
    };
}
