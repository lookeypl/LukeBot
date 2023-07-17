using LukeBot.Common;
using LukeBot.Module;

namespace LukeBot
{
    public class ModuleDisabledException: Exception
    {
        public ModuleDisabledException(ModuleType module, string user)
            : base(string.Format("Module {0} is already disabled for user {1}", module, user))
        {
        }
    }
}