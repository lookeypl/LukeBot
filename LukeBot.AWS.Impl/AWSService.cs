using System.Collections.Generic;
using LukeBot.AWS;
using LukeBot.Common;
using LukeBot.Logging;
using Amazon.Auth;


namespace LukeBot.AWS.Impl
{
    public class AWSService : IAWSService
    {
        Polly mPolly;

        private AWSService()
        {
        }

        static public IAWSService Create()
        {
            return new AWSService();
        }

        public string GetServiceDebugName()
        {
            return Common.Constants.AWS_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<string>();
        }

        public IPolly Polly()
        {
            return mPolly;
        }

        public void RequestShutdown()
        {
        }

        public void Run()
        {
            mPolly = new();

            Logger.Log().Info("AWS Service layer running");
        }

        public void WaitForShutdown()
        {
        }
    }
}
