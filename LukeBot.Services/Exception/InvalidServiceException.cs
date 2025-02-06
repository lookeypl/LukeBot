using LukeBot.Common;

namespace LukeBot.Services
{
    public class InvalidServiceException: Exception
    {
        public InvalidServiceException(string serviceType)
            : base(string.Format("Invalid service type provided - {0}", serviceType))
        {
        }
    }
}
