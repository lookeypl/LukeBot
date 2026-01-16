using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public sealed class IntermediaryService: IIntermediaryService
    {
        private Dictionary<string, Intermediary> mIntermediaries = new();

        private IntermediaryService()
        {
        }

        public static IIntermediaryService Create()
        {
            return new IntermediaryService();
        }

        public void Register(string service)
        {
            mIntermediaries.Add(service, new Intermediary());
        }

        public IIntermediary GetIntermediary(string service)
        {
            return mIntermediaries[service];
        }

        public string GetServiceName()
        {
            return Constants.INTERMEDIARY_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<string>();
        }

        public void Run()
        {
            // noop
        }

        public void RequestShutdown()
        {
            // noop
        }

        public void WaitForShutdown()
        {
            // noop
        }
    }
}
