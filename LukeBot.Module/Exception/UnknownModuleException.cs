using LukeBot.Common;

namespace LukeBot.Module
{
    public class UnknownModuleException: Exception
    {
        public UnknownModuleException(string moduleName)
            : base(string.Format("Unrecognized module name {0}", moduleName))
        {
        }
    }
}
