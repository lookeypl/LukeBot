using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LukeBot.UI
{
    public class Interface
    {
        public static void ThreadMain()
        {
            Interface iface = new Interface();
            iface.Run();
        }

        public void Run()
        {
            CreateHostBuilder().Build().Run();
        }

        public IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
