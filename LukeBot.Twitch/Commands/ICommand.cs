using LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public abstract class ICommand
    {
        protected string mName;
        protected User mPrivilegeLevel;

        protected ICommand(Descriptor d)
        {
            mName = d.Name;
            mPrivilegeLevel = d.Privilege;
        }

        protected ICommand(string name, User privilegeLevel)
        {
            mName = name;
            mPrivilegeLevel = privilegeLevel;
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
            //   B M V S C
            //   1 0 0 0 0  priv
            return (userIdentity & mPrivilegeLevel) > 0;
        }
    }
}
