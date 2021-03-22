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

        public void Stop()
        {
            Task stop = mHost.StopAsync();
            stop.Wait();
        }

        public IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
