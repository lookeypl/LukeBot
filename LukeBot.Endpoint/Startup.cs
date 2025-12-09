using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using Intercom = LukeBot.Communication.Common.Intercom;
using LukeBot.Services;
using LukeBot.Widget.Common;
using Microsoft.Extensions.FileProviders;
using System.Net.Http;
using NgrokExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace LukeBot.Endpoint
{
    public class Startup
    {
        private string mTTSEndpoint = "";

        private IWidgetService GetWidgetService()
        {
            return Service.Get(Constants.WIDGET_SERVICE_NAME) as IWidgetService;
        }

        async Task LoadPage(string page, HttpContext context)
        {
            StreamReader reader = File.OpenText("Pages/" + page);
            string p = reader.ReadToEnd();
            reader.Close();

            context.Response.Headers.ContentType = "text/html";
            await context.Response.WriteAsync(p);
        }

        async Task LoadJSPage(string page, HttpContext context)
        {
            StreamReader reader = File.OpenText("Pages/" + page);
            string p = reader.ReadToEnd();
            reader.Close();

            context.Response.Headers.ContentType = "text/javascript";
            await context.Response.WriteAsync(p);
        }

        async Task HandleAPICall(string call, HttpContext context)
        {
            string responseString;

            if (call == "Users")
            {
                UsersResponse response = new UsersResponse();
                response.status = 0;
                response.users.Add(new Common.UserItem{
                    name = "lookey",
                    displayName = "Looki",
                });
                response.users.Add(new Common.UserItem{
                    name = "michakes",
                    displayName = "Michie",
                });

                responseString = JsonSerializer.Serialize(response);
            }
            else
            {
                ResponseBase response = new ResponseBase{
                    status = 1,
                };

                responseString = JsonSerializer.Serialize(response);
            }

            await context.Response.WriteAsync(responseString);
        }

        async Task HandleServiceCallback(string service, HttpContext context)
        {
            Logger.Log().Info("Received callback for service " + service + ": " + context.Request.Path.Value);
            Logger.Log().Debug("We have " + context.Request.Query.Count + " queries");
            if (Logger.IsLogLevelEnabled(LogLevel.Secure))
            {
                foreach (var query in context.Request.Query)
                {
                    Logger.Log().Secure("  -> " + query.Key + " = " + query.Value);
                }
            }

            Intermediary srv = Comms.Intermediary.GetIntermediary(service);

            if (!context.Request.Query.ContainsKey("state"))
            {
                Logger.Log().Error("Received back no state. This should not happen.");
                return;
            }

            context.Response.Headers.ContentType = "text/html";

            string state = context.Request.Query["state"];

            if (context.Request.Query.ContainsKey("error"))
            {
                srv.Reject(state);
                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " rejected: " + context.Request.Query["error_description"] + '\n' +
                    "</body></html>"
                );
                return;
            }

            try
            {
                UserToken token = new UserToken();
                token.code = context.Request.Query["code"];
                token.state = state;

                srv.Fulfill(token.state, token);

                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " successful, you can close the window now.\n" +
                    "</body></html>"
                );
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0}", e.Message);
                srv.Reject(state);
                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " failed. Check log for details.\n" +
                    "</body></html>"
                );
            }
        }

        async Task HandleWidgetCallback(string widgetUUID, HttpContext context)
        {
            Logger.Log().Debug("Widget requested - handling {0}", widgetUUID);

            try
            {
                IWidgetUserModule wum = GetWidgetService().GetModuleByWidgetUUID(widgetUUID);
                string pageContents = wum.GetWidgetPage(widgetUUID);
                context.Response.Headers.ContentType = "text/html";
                await context.Response.WriteAsync(pageContents);
            }
            catch (System.Exception e)
            {
                await context.Response.WriteAsync("Couldn't load widget: " + e.Message);
            }
        }

        async Task HandleWidgetWSCallback(string widgetUUID, HttpContext context)
        {
            Logger.Log().Debug("Widget WS connection requested - handling {0}", widgetUUID);

            if (!context.WebSockets.IsWebSocketRequest)
            {
                Logger.Log().Warning("Connection to WebSocket endpoint is not a WS request! Aborting");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            try
            {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();

                IWidgetUserModule wum = GetWidgetService().GetModuleByWidgetUUID(widgetUUID);
                Task lifetimeTask = wum.AssignWidgetWebSocket(widgetUUID, ws);

                Logger.Log().Debug("Awaiting lifetime task to keep connection to {0} Widget WS alive", widgetUUID);
                // await for ws to complete, it will be closed in IWidget.cs when needed
                await lifetimeTask;
                Logger.Log().Debug("Lifetime task for Widget WS {0} finished", widgetUUID);

                // TODO at this point Kestrel logs "the application completed without reading the entire request body."
                // I'm not sure why this happens, probably should be taken care of but I couldn't find any reason why
                // or how to remedy it.
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Error while processing WS connection for widget: {0}", e.Message);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        async Task HandleTTSCallback(HttpContext context)
        {
            // TODO this endpoint MUST validate the request came from an active widget
            if (mTTSEndpoint == "")
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await context.Response.WriteAsync("TTS Endpoint not available");
                return;
            }

            if (!context.Request.Query.ContainsKey("voice") ||
                !context.Request.Query.ContainsKey("text"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("Cannot generate TTS, arguments invalid");
                return;
            }

            string voice = context.Request.Query["voice"];
            string text = context.Request.Query["text"];

            HttpClient client = new HttpClient();

            Dictionary<string, string> query = new();
            query.Add("voice", voice);
            query.Add("text", text);

            UriBuilder builder = new UriBuilder(new Uri(mTTSEndpoint));
            builder.Query += String.Join('&', query.Select(x => x.Key + '=' + x.Value).ToArray());

            HttpResponseMessage ttsFetchResponse = await client.GetAsync(builder.ToString());

            if (!ttsFetchResponse.IsSuccessStatusCode)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(String.Format("Failed to fetch TTS: {0} ({1})", ttsFetchResponse.StatusCode, ttsFetchResponse.ReasonPhrase));
                return;
            }

            byte[] data = await ttsFetchResponse.Content.ReadAsByteArrayAsync();

            // BIG TODO
            // This step requires some sort of file manager
            // We should cache these results somewhere, maybe invalidate them after some time
            // And forward a path to resource that the Widget can use

            // forward data to the response
            context.Response.ContentType = "audio/ogg";
            await context.Response.BodyWriter.WriteAsync(data);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!Conf.TryGet<string>(Common.Constants.PROP_STORE_TTS_ENDPOINT_PROP, out mTTSEndpoint))
            {
                Logger.Log().Warning("TTS Endpoint not configured, TTS not available");
            }

            app.UseHttpsRedirection();
            app.UseWebSockets();
            app.UseRouting();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory() + "/Data/ContentRoot"),
                RequestPath = "/content"
            });
            app.UseEndpoints(endpoints =>
            {
                /* TODO: Disabled while the web UI part is not ready
                endpoints.MapGet("/", async context => {
                    await LoadPage("index.html", context);
                });
                endpoints.MapGet("css/{stylesheet}", async context => {
                    var stylesheet = context.Request.RouteValues["stylesheet"];
                    await LoadPage($"css/{stylesheet}", context);
                });*/
                endpoints.MapGet("js/{script}", async context => {
                    var script = context.Request.RouteValues["script"];
                    await LoadJSPage($"js/{script}", context);
                });
                /*endpoints.MapGet("views/{view}", async context => {
                    var view = context.Request.RouteValues["view"];
                    await LoadPage($"views/{view}", context);
                });
                endpoints.MapGet("api/{call}", async context => {
                    var call = context.Request.RouteValues["call"];
                    await HandleAPICall($"{call}", context);
                });*/
                endpoints.MapGet("callback/{service}", async context => {
                    var service = context.Request.RouteValues["service"];
                    await HandleServiceCallback($"{service}", context);
                });
                endpoints.MapGet("widget/{widget}", async context => {
                    var widget = context.Request.RouteValues["widget"];
                    await HandleWidgetCallback($"{widget}", context);
                });
                endpoints.Map("widgetws/{widget}", async context => {
                    var widget = context.Request.RouteValues["widget"];
                    await HandleWidgetWSCallback($"{widget}", context);
                });
                endpoints.Map("widget/tts", async context => {
                    await HandleTTSCallback(context);
                });
            });
        }
    }
}
