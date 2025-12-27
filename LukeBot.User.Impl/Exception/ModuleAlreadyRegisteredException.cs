using LukeBot.Common;

namespace LukeBot.User.Impl
{
    public class ModuleAlreadyAttachedException: Exception
    {
        public ModuleAlreadyAttachedException(string type)
            : base(string.Format("Module {0} already attached", type))
        {
        }
    }
}
