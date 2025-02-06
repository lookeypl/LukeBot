using LukeBot.Common;

namespace LukeBot.Services
{
    public class UnknownServiceException: Exception
    {
        public UnknownServiceException(string name)
            : base(string.Format("Unrecognized service \"{0}\"", name))
        {
        }
    }
}
