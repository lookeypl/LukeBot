using LukeBot.Common;

namespace LukeBot.User.Impl
{
    public class ModuleNotAvailableException: Exception
    {
        public ModuleNotAvailableException(string module, string user)
            : base(string.Format("User Module {0} is not available for user {1}", module, user))
        {
        }
    }
}