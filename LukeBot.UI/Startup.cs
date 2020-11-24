using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;

namespace LukeBot.UI
{
    public class Startup
    {
        async Task LoadPage(string page, HttpContext context)
        {
            StreamReader reader = File.OpenText("LukeBot.UI/Pages/" + page);
            string p = reader.ReadToEnd();
            reader.Close();
            await context.Response.WriteAsync(p);
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => {
                    await LoadPage("index.html", context);
                });
                endpoints.MapGet("/{page:alpha}", async context => {
                    var page = context.Request.RouteValues["page"];
                    await LoadPage($"{page}.html", context);
                });
                endpoints.MapGet("css/{stylesheet}", async context => {
                    var stylesheet = context.Request.RouteValues["stylesheet"];
                    await LoadPage($"css/{stylesheet}", context);
                });
                endpoints.MapGet("js/{script}", async context => {
                    var script = context.Request.RouteValues["script"];
                    await LoadPage($"js/{script}", context);
                });
                endpoints.MapGet("views/{view}", async context => {
                    var view = context.Request.RouteValues["view"];
                    await LoadPage($"views/{view}", context);
                });
            });
        }
    }
}
