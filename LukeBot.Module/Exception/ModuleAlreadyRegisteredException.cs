using LukeBot.Common;

namespace LukeBot.Module
{
    public class ModuleAlreadyRegisteredException: Exception
    {
        public ModuleAlreadyRegisteredException(ModuleType type)
            : base(string.Format("Module {0} already registered", type))
        {
        }
    }
}
