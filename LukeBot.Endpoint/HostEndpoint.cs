using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Config;
using System.Net;


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

        public static void AddUrl(string domain, int port, ref List<string> URLs)
        {
            if (port == LukeBot.Common.Constants.DEFAULT_SERVER_PORT)
            {
                URLs.Add(String.Format("https://{0}/", domain));
            }
            else
            {
                URLs.Add(String.Format("https://{0}:{1}/", domain, port));
            }
        }

        public IHostBuilder CreateHostBuilder()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();

            List<string> URLs = new();
            string domain = LukeBot.Common.Constants.DEFAULT_SERVER_HTTPS_DOMAIN;
            int port = LukeBot.Common.Constants.DEFAULT_SERVER_PORT;

            Conf.TryGet<string>(Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP, out domain);
            Conf.TryGet<int>(Common.Constants.PROP_STORE_SERVER_PORT_PROP, out port);

            AddUrl(domain, port, ref URLs);

            if (!domain.Contains("localhost"))
            {
                // add localhost for local testing purposes
                AddUrl("localhost", port, ref URLs);
            }

            Logger.Log().Info("Endpoint using host addresses:");
            foreach (string addr in URLs)
            {
                Logger.Log().Info("  - " + addr);
            }

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(URLs.ToArray());
                webBuilder.UseStartup<Startup>();
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory() + "/Data/ContentRoot");
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LBLoggingProvider());
            });

            builder.ConfigureServices(services =>
            {
                // LettuceEncrypt is not needed when domain is set to localhost
                // we assume we're in dev environment which has dev certificate provided
                if (!domain.Contains("localhost"))
                {
                    string email = Conf.Get<string>(Common.Constants.PROP_STORE_HTTPS_EMAIL_PROP);

                    Logger.Log().Info("Configuring LettuceEncrypt for domain {0} email {1}", domain, email);
                    services.AddLettuceEncrypt(c =>
                    {
                        c.AcceptTermsOfService = true;
                        c.DomainNames = new string[] { domain };
                        c.EmailAddress = email;
                    });
                }
                else
                {
                    Logger.Log().Warning("=== NOTE ===");
                    Logger.Log().Warning("HTTPS domain is set to localhost - assuming we're in dev environment");
                    Logger.Log().Warning("If something fails, remember to run \"dotnet dev-certs https --trust\"");
                    #if (LINUX)
                    Logger.Log().Warning("=== LINUX-SPECIFIC NOTE ===");
                    Logger.Log().Warning("On Linux even this might not work, as dotnet dev-certs only trusts certificates user-side.");
                    Logger.Log().Warning("An example of potential issues would be OBS running from Flatpak not loading any LukeBot Widgets.");
                    Logger.Log().Warning("To fix this, call above command and then add dotnet dev-cert system-wide (requires root access) with:");
                    Logger.Log().Warning("  dotnet tool update -g linux-dev-certs");
                    Logger.Log().Warning("  dotnet linux-dev-certs install");
                    #endif
                    Logger.Log().Warning("============");
                }
            });

            return builder;
        }
    }
}
