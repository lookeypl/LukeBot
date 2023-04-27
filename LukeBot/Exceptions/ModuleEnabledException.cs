using LukeBot.Common;

namespace LukeBot
{
    public class ModuleEnabledException: Exception
    {
        public ModuleEnabledException(string module, string user)
            : base(string.Format("Module {0} already enabled for user {1}", module, user))
        {
        }
    }
}