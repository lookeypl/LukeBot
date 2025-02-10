using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Config;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Https;


namespace LukeBot.Endpoint
{
    public class HostEndpoint
    {
        private IHost mHost = null;

        public async void Start()
        {
            mHost = CreateHostBuilder().Build();
            await mHost.StartAsync();
        }

        public async void Stop()
        {
            if (mHost != null)
                await mHost.StopAsync();
        }

        public IHostBuilder CreateHostBuilder()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();

            string domain;
            string[] URLs;

            if (!Conf.TryGet<string>(Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP, out domain))
            {
                domain = "localhost";
            }

            if (domain.Contains("localhost"))
            {
                // manually set only localhost
                // we do this path just in case someone prefers to use different-than-default port 5000
                URLs = new string[]
                {
                    "https://" + domain + "/",
                };
            }
            else
            {
                // add defined address + localhost:5000
                URLs = new string[]
                {
                    "https://" + domain + "/",
                    "https://localhost:5000/"
                };
            }

            Logger.Log().Info("Endpoint using host addresses:");
            foreach (string addr in URLs)
            {
                Logger.Log().Info("  - " + addr + "/");
            }

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(URLs);
                webBuilder.UseStartup<Startup>();
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory() + "/Data/ContentRoot");
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LBLoggingProvider());
            });

            return builder;
        }
    }
}
