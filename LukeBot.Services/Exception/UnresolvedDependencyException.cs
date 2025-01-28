using LukeBot.Common;

namespace LukeBot.Services
{
    public class UnresolvedDependencyException: Exception
    {
        public UnresolvedDependencyException(string service, string dependency)
            : base(string.Format("Service {0} missing dependency \"{1}\"", service, dependency))
        {
        }
    }
}
