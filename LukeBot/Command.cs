using LukeBot.User;

namespace LukeBot
{
    internal abstract class Command
    {
        public PermissionLevel PermissionLevel { get; private set; }

        public Command(PermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }

        public bool IsPermitted(PermissionLevel userLevel)
        {
            return userLevel >= PermissionLevel;
        }

        public abstract string Execute(CLIMessageProxy cliProxy, string[] args);
    }
}