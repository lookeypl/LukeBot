using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LukeBot.UI
{
    public class Interface
    {
        IHost mHost = null;

        public static void ThreadMain()
        {
            Interface iface = new Interface();
            iface.Run();
        }

        public void Run()
        {
            mHost = CreateHostBuilder().Build();
            mHost.Run();
        }

        public async void Stop()
        {
            await mHost.StopAsync();
        }

        public IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    string IP = Common.Utils.GetConfigServerIP();
                    if (IP != Common.Constants.DEFAULT_SERVER_IP)
                    {
                        string[] URLs = new string[]
                        {
                            // TODO readd below with certificates
                            //"https://" + IP + "/",
                            "http://" + IP + ":5000/",
                        };
                        webBuilder.UseUrls(URLs);
                    }
                    webBuilder.UseStartup<Startup>();
                });
    }
}
