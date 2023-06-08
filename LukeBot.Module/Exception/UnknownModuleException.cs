using LukeBot.Common;

namespace LukeBot.Module
{
    public class UnknownModuleException: Exception
    {
        public UnknownModuleException(ModuleType type)
            : base(string.Format("Unrecognized module type {0}", type))
        {
        }
    }
}
