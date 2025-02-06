using LukeBot.Common;

namespace LukeBot.Services
{
    public class UnresolvedServiceDependencyException: Exception
    {
        public UnresolvedServiceDependencyException(string service, string dependency)
            : base(string.Format("Service {0} missing dependency \"{1}\"", service, dependency))
        {
        }
    }
}
