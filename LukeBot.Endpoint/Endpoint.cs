using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using LukeBot.Common;
using LukeBot.Config;


namespace LukeBot.Endpoint
{
    public class Endpoint
    {
        private static Endpoint mEndpoint = null;
        private static Thread mEndpointThread = null;
        private IWebHost mHost = null;

        private static void ThreadMain()
        {
            mEndpoint = new Endpoint();
            mEndpoint.Run();
        }

        public static void StartThread()
        {
            mEndpointThread = new Thread(ThreadMain);
            mEndpointThread.Start();
        }

        public static void StopThread()
        {
            if (mEndpoint != null)
                mEndpoint.Stop();

            if (mEndpointThread != null)
                mEndpointThread.Join();
        }

        public void Run()
        {
            mHost = CreateHostBuilder().Build();
            mHost.Run();
        }

        public async void Stop()
        {
            if (mHost != null)
                await mHost.StopAsync();
        }

        public IWebHostBuilder CreateHostBuilder()
        {
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder();

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LBLoggingProvider());
            });

            string IP = Conf.Get<string>(Common.Constants.PROP_STORE_SERVER_IP_PROP);
            if (IP != Common.Constants.DEFAULT_SERVER_IP)
            {
                string[] URLs = new string[]
                {
                    // TODO readd below with certificates
                    //"https://" + IP + "/",
                    "http://" + IP + "/",
                    //"https://localhost:5001/",
                    "http://localhost:5000/",
                };
                Logger.Log().Debug("Using host IP " + IP);
                builder.UseUrls(URLs);
            }

            builder.UseStartup<Startup>();

            return builder;
        }
    }
}
