using LukeBot.Common;

namespace LukeBot.Services
{
    public class ServiceRunFailedException: Exception
    {
        public ServiceRunFailedException(string name, System.Exception e)
            : base(string.Format("Failed to run Services - service {0} raised an Exception", name), e)
        {
        }
    }
}
