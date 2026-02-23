using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using LukeBot.Common;
using LukeBot.Logging;


namespace LukeBot.Widget.Impl
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
        private class EchoMessage: SerializableEventArgsBase
        {
            public string Message { get; set; }

            public EchoMessage(string message)
                : base("EchoMessage")
            {
                Message = message;
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<EchoMessage>(this);
            }
        }

        private WidgetResponse mReceivedResponse = null;
        private AutoResetEvent mReceiveEvent = new(false);

        protected override void OnReceivedResponse(WidgetResponse response)
        {
            mReceivedResponse = response;
            mReceiveEvent.Set();
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
            Logger.Log().Info("Echoing: {0}", secretEchoMessage);

            EchoMessage msg = new EchoMessage(secretEchoMessage);
            SendToWS(msg);

            mReceiveEvent.WaitOne();
            if (mReceivedResponse == null)
            {
                Logger.Log().Error("Echo failed - received null response");
                return;
            }

            if (mReceivedResponse.ErrorCount != 1)
            {
                Logger.Log().Error("Echo failed - received response has invalid error count ({0}, expected 1)", mReceivedResponse.ErrorCount);
            }

            if (mReceivedResponse.Reason[0] == msg.Message)
            {
                Logger.Log().Info("Echo successful");
            }
            else
            {
                Logger.Log().Error("Echo did not return the same message: expected {0}; received {1}",
                    msg.Message, mReceivedResponse.Reason[0]);
            }
        }

        protected override void OnDisconnected()
        {
        }

        protected override void OnLoad()
        {
        }

        protected override void OnUnload()
        {
        }

        protected override ConfigurationBase CreateDefaultConfiguration()
        {
            return new EmptyWidgetConfiguration();
        }

        public Echo(string lbUser, string id, string name)
            : base(lbUser, "Widgets/Echo.html", id, name)
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