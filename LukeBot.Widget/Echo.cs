using System;
using System.Linq;
using LukeBot.Logging;
using LukeBot.Widget.Common;
using Newtonsoft.Json;


namespace LukeBot.Widget
{
    /**
     * Simple Echo "widget" which is meant only for WebSocket communication testing.
     *
     * When this widget is created and opened in a browser (or OBS) it does not display
     * anything. Instead, it only relays messages back to the server via created websocket
     * connection and checks if the same "message" was sent back.
     */
    public class Echo: IWidget
    {
        private class EchoMessage
        {
            public string EventName { get; set; }
            public string Message { get; set; }

            public EchoMessage(string message)
            {
                EventName = "EchoMessage";
                Message = message;
            }
        }

        private class EchoResponse
        {
            public string Message { get; set; }
        }

        protected override void OnConnected()
        {
            Random random = new Random();

            const string chars = "abcdefghijklmnopqrstuvwxyz123456789";
            string secretEchoMessage = new string(
                Enumerable.Repeat(chars, 12)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray()
            );
            EchoMessage msg = new EchoMessage(secretEchoMessage);

            Logger.Log().Info("Echoing: {0}", secretEchoMessage);
            SendToWSAsync(JsonConvert.SerializeObject(msg));

            EchoResponse resp = JsonConvert.DeserializeObject<EchoResponse>(RecvFromWS());
            if (resp.Message == msg.Message)
            {
                Logger.Log().Info("Echo successful");
            }
            else
            {
                Logger.Log().Error("Echo did not return the same message: expected {0}; received {1}",
                    msg.Message, resp.Message);
            }
        }

        public Echo(string id, string name)
            : base("LukeBot.Widget/Widgets/Echo.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.echo;
        }

        ~Echo()
        {
        }
    }
}