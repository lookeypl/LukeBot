using LukeBot.Common;

namespace LukeBot.Module
{
    public class ModuleAlreadyRegisteredException: Exception
    {
        public ModuleAlreadyRegisteredException(string moduleName)
            : base(string.Format("Module {0} already registered", moduleName))
        {
        }
    }
}
