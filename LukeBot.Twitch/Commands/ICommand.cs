using LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public abstract class ICommand
    {
        internal delegate void UpdateConfigDelegate(string commandName);

        protected string mName;
        protected User mPrivilegeLevel;
        protected bool mEnabled;
        private UpdateConfigDelegate mUpdateConfig;

        protected ICommand(Descriptor d)
        {
            mName = d.Name;
            mPrivilegeLevel = d.Privilege;
            mEnabled = d.Enabled;
        }

        protected ICommand(string name, User privilegeLevel)
        {
            mName = name;
            mPrivilegeLevel = privilegeLevel;
            mEnabled = true;
        }

        // To be called from within Command's Execute() call. Triggers a Config update.
        protected void UpdateConfig()
        {
            mUpdateConfig(mName);
        }

        internal void SetUpdateConfigDelegate(UpdateConfigDelegate d)
        {
            mUpdateConfig = d;
        }

        // Provides:
        //  * args - arguments from a chat message incl. called command name;
        //  * callerPrivilege - caller's (person who called the command) User privilege flags.
        //
        // Returns a string - response that TwitchIRC will send further back to Twitch servers
        // assuming it's not empty (empty string will keep bot silent)
        public abstract string Execute(User callerPrivilege, string[] args);

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

        public void SetEnabled(bool enabled)
        {
            mEnabled = enabled;
        }

        public bool IsEnabled()
        {
            return mEnabled;
        }
    }
}
