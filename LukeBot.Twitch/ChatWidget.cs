using System.IO;
using System.Text.Json;
using LukeBot.Common;

namespace LukeBot.Twitch
{
    class ChatWidget: IWidget
    {
        TwitchIRC mIRC;
        ConnectionPort mPort;
        WebSocketServer mServer;

        private void OnMessage(object o, TwitchIRCMessage args)
        {
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(args));
        }

        public ChatWidget(TwitchIRC IRC)
            : base()
        {
            mIRC = IRC;

            mPort = ConnectionManager.Instance.AcquirePort();
            Logger.Debug("Widget will have port {0}", mPort.Value);

            mIRC.Message += OnMessage;

            AddToHead(string.Format("<meta name=\"widgetport\" content=\"{0}\">", mPort.Value));

            mServer = new WebSocketServer("127.0.0.1", mPort.Value);
        }

        ~ChatWidget()
        {
        }

        protected override string GetWidgetCode()
        {
            if (mServer.Running)
            {
                mServer.RequestShutdown();
                mServer.WaitForShutdown();
            }

            StreamReader reader = File.OpenText("LukeBot.Twitch/Widgets/Chat.html");
            string p = reader.ReadToEnd();
            reader.Close();

            mServer.Start();

            return p;
        }

        public override void RequestShutdown()
        {
            mServer.RequestShutdown();
        }

        public override void WaitForShutdown()
        {
            if (mServer.Running)
            {
                mServer.WaitForShutdown();
            }
        }
    }
}
