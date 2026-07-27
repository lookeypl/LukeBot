namespace LukeBot.Twitch.Command
{
    public abstract class ICommand
    {
        public delegate void UpdateConfigDelegate(string commandName);

        protected string mName;
        protected ChatUser mPrivilegeLevel;
        protected bool mEnabled;
        private UpdateConfigDelegate mUpdateConfig;

        protected ICommand(Descriptor d)
        {
            mName = d.Name;
            mPrivilegeLevel = d.Privilege;
            mEnabled = d.Enabled;
        }

        protected ICommand(string name, ChatUser privilegeLevel)
        {
            mName = name;
            mPrivilegeLevel = privilegeLevel;
            mEnabled = true;
        }

        // To be called from within Command's Execute() call. Triggers a Config update.
        protected void UpdateConfig()
        {
            if (mUpdateConfig != null)
            {
                mUpdateConfig(mName);
            }
        }

        // to call in-command whether the privilege matches allowed combination
        // true if check passed (userIdentity is a part of allowed), false if user is not allowed
        protected bool CheckPrivilege(ChatUser userIdentity, ChatUser allowed)
        {
            //   B M V S C
            //   1 0 0 0 0  priv
            return (userIdentity & allowed) > 0;
        }

        public void SetUpdateConfigDelegate(UpdateConfigDelegate d)
        {
            mUpdateConfig = d;
        }

        // Provides:
        //  * args - arguments from a chat message incl. called command name;
        //  * callerPrivilege - caller's (person who called the command) ChatUser privilege flags.
        //
        // Returns a string - response that TwitchIRC will send further back to Twitch servers
        // assuming it's not empty (empty string will keep bot silent)
        public abstract string Execute(ChatUser callerPrivilege, string[] args);

        // Edit command output based on newValue param
        public abstract void Edit(string newValue);

        public abstract Descriptor ToDescriptor();

        public void AllowUsers(ChatUser u)
        {
            mPrivilegeLevel |= u;
        }

        public void DenyUsers(ChatUser u)
        {
            mPrivilegeLevel &= ~u;
        }

        // checks privilege against command's privilege level
        public bool CheckPrivilege(ChatUser userIdentity)
        {
            return CheckPrivilege(userIdentity, mPrivilegeLevel);
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
