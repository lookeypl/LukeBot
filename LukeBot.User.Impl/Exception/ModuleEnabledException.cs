using LukeBot.Common;

namespace LukeBot.User.Impl
{
    public class ModuleEnabledException: Exception
    {
        public ModuleEnabledException(string moduleType, string user)
            : base(string.Format("Module {0} already enabled for user {1}", moduleType, user))
        {
        }
    }
}