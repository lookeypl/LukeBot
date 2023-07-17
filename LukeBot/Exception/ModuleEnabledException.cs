using LukeBot.Common;
using LukeBot.Module;

namespace LukeBot
{
    public class ModuleEnabledException: Exception
    {
        public ModuleEnabledException(ModuleType module, string user)
            : base(string.Format("Module {0} already enabled for user {1}", module, user))
        {
        }
    }
}