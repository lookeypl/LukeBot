using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


namespace LukeBot.Endpoint
{
    public class Interface
    {
        IWebHost mHost = null;

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

        public IWebHostBuilder CreateHostBuilder()
        {
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder();

            string IP = Common.Utils.GetConfigServerIP();
            if (IP != Common.Constants.DEFAULT_SERVER_IP)
            {
                string[] URLs = new string[]
                {
                    // TODO readd below with certificates
                    //"https://" + IP + "/",
                    "http://" + IP + ":5000/",
                };
                builder.UseUrls(URLs);
            }

            builder.UseStartup<Startup>();

            return builder;
        }
    }
}
