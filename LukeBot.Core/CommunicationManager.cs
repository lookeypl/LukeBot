using System;
using System.Collections.Generic;


namespace LukeBot.Core
{
    public sealed class CommunicationManager
    {
        private static readonly Lazy<CommunicationManager> mInstance =
            new Lazy<CommunicationManager>(() => new CommunicationManager());
        public static CommunicationManager Instance { get { return mInstance.Value; } }

        private Dictionary<string, Intermediary> mServices = new Dictionary<string, Intermediary>();

        private CommunicationManager()
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
