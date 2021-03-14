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
                // TODO workaround - a launch argument would be a good solution for this
                #if (DEBUG)
                return String.Format("Unrecognized command: {0}", cmd);
                #else
                return "";
                #endif
            }

            return mCommands[cmd].Execute(args);
        }

        public void AddCommand(string name, Command.ICommand command)
        {
            if (mCommands.ContainsKey(name))
                throw new ArgumentException(String.Format("Command {0} already exists for channel {1}", name, mChannelName));

            mCommands.Add(name, command);
        }
    };
}
