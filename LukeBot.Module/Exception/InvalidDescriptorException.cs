using LukeBot.Common;

namespace LukeBot.Module
{
    public class InvalidDescriptorException: Exception
    {
        public InvalidDescriptorException(string reason)
            : base(string.Format("Invalid module descriptor provided - {0}", reason))
        {
        }

        public InvalidDescriptorException(ModuleType type, string reason)
            : base(string.Format("Invalid descriptor provided for module {0} - {1}", type, reason))
        {
        }
    }
}
