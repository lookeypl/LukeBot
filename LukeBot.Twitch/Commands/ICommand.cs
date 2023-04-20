using LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public abstract class ICommand
    {
        protected string mName;
        protected User mPrivilegeLevel;

        protected ICommand(string name)
        {
            mName = name;
            mPrivilegeLevel = User.Everyone;
        }

        // Provided args from a chat message; returns a message to send back
        // That way TwitchIRC will send it further back to Twitch servers
        public abstract string Execute(string[] args);

        // Edit command output based on newValue param
        public abstract void Edit(string newValue);

        public abstract Descriptor ToDescriptor();

        public void AllowUsers(User u)
        {
            mPrivilegeLevel |= u;
        }

        public void DenyUsers(User u)
        {
            mPrivilegeLevel &= ~u;
        }

        public bool CheckPrivilege(User userIdentity)
        {
            return (userIdentity & mPrivilegeLevel) > 0;
        }
    }
}
