using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.Services
{
    public class CircularServiceDependencyException: Exception
    {
        List<string> mChain = new();

        internal CircularServiceDependencyException(string service, CircularServiceDependencyChain chain)
            : base(string.Format("While resolving service {0} dependencies found a circular dependency: {1}", service, chain.FormMessage()))
        {
        }
    }
}
