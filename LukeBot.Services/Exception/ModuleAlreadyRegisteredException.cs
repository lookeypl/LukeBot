using LukeBot.Common;

namespace LukeBot.Services
{
    public class ModuleAlreadyRegisteredException: Exception
    {
        public ModuleAlreadyRegisteredException(string type)
            : base(string.Format("Module {0} already registered", type))
        {
        }
    }
}
