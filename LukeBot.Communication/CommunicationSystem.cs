using System;
using System.Collections.Generic;


namespace LukeBot.Communication
{
    public sealed class CommunicationSystem
    {
        private Dictionary<string, Intermediary> mServices = new Dictionary<string, Intermediary>();

        public CommunicationSystem()
        {
        }

        public void Register(string service)
        {
            mServices.Add(service, new Intermediary());
        }

        public Intermediary GetIntermediary(string service)
        {
            return mServices[service];
        }
    }
}
