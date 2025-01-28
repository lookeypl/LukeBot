using LukeBot.Common;

namespace LukeBot.Services
{
    public class ServiceAlreadyRegisteredException: Exception
    {
        public ServiceAlreadyRegisteredException(string s)
            : base(string.Format("Service {0} already registered", s))
        {
        }
    }
}
