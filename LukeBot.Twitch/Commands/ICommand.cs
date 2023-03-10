using LukeBot.Twitch.Common;


namespace LukeBot.Twitch.Command
{
    public abstract class ICommand
    {
        protected string mName;

        protected ICommand(string name)
        {
            mName = name;
        }

        // Provided args from a chat message; returns a message to send back
        // That way TwitchIRC will
        public abstract string Execute(string[] args);

        // Edit command output based on newValue param
        public abstract void Edit(string newValue);

        public abstract Descriptor ToDescriptor();
    }
}
