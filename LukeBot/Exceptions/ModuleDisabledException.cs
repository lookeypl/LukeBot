using LukeBot.Common;

namespace LukeBot
{
    public class ModuleDisabledException: Exception
    {
        public ModuleDisabledException(string module, string user)
            : base(string.Format("Module {0} is already disabled for user {1}", module, user))
        {
        }
    }
}