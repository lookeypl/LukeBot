using LukeBot.Common;

namespace LukeBot.Services
{
    public class InvalidDescriptorException: Exception
    {
        public InvalidDescriptorException(string reason)
            : base(string.Format("Invalid module descriptor provided - {0}", reason))
        {
        }

        public InvalidDescriptorException(string type, string reason)
            : base(string.Format("Invalid descriptor provided for module {0} - {1}", type, reason))
        {
        }
    }
}
