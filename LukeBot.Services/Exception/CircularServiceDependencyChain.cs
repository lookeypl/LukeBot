using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.Services
{
    internal class CircularServiceDependencyChain: System.Exception
    {
        private List<string> mChain = new();

        internal CircularServiceDependencyChain()
        {}

        internal void Add(string service)
        {
            mChain.Add(service);
        }

        internal string FormMessage()
        {
            string ret = "";

            foreach (string s in mChain)
            {
                ret += s;
                ret += " -> ";
            }

            ret += mChain[0];
            return ret;
        }
    }
}
