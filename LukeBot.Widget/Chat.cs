using System.IO;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Core;
using LukeBot.Core.Events;


namespace LukeBot.Widget
{
    public class Chat: IWidget
    {
        ConnectionPort mPort;
        WebSocketServer mServer;

        private void OnMessage(object o, EventArgsBase args)
        {
            TwitchChatMessageArgs a = (TwitchChatMessageArgs)args;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(a));
        }

        private void OnClearChat(object o, EventArgsBase args)
        {
            TwitchChatUserClearArgs a = (TwitchChatUserClearArgs)args;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(a));
        }

        private void OnClearMsg(object o, EventArgsBase args)
        {
            TwitchChatMessageClearArgs a = (TwitchChatMessageClearArgs)args;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(a));
        }

        public Chat()
            : base()
        {
            mPort = Systems.Connection.AcquirePort();
            Logger.Log().Debug("Widget will have port {0}", mPort.Value);

            Core.Systems.Event.TwitchChatMessage += OnMessage;
            Core.Systems.Event.TwitchChatUserClear += OnClearChat;
            Core.Systems.Event.TwitchChatMessageClear += OnClearMsg;

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);
        }

        ~Chat()
        {
        }

        protected override string GetWidgetCode()
        {
            if (mServer.Running)
            {
                mServer.RequestShutdown();
                mServer.WaitForShutdown();
            }

            StreamReader reader = File.OpenText("LukeBot.Widget/Widgets/Chat.html");
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
