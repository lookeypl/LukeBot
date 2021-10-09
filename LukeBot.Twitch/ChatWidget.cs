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

        private void OnClearChat(object o, TwitchIRCClearChat args)
        {
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(args));
        }

        private void OnClearMsg(object o, TwitchIRCClearMsg args)
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
            mIRC.ClearChat += OnClearChat;
            mIRC.ClearMsg += OnClearMsg;

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            WidgetManager.Instance.Register(this, "TEST-CHAT-WIDGET");
            Logger.Secure("Registered Chat widget at link http://{0}/widget/{1}; WS port {2}", serverIP, ID, mPort.Value);
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
