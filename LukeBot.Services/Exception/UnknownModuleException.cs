using LukeBot.Common;

namespace LukeBot.Services
{
    public class UnknownModuleException: Exception
    {
        public UnknownModuleException(string type)
            : base(string.Format("Unrecognized module type {0}", type))
        {
        }
    }
}
