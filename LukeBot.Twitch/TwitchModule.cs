using LukeBot.Common;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        TwitchIRC mIRC;
        ChatWidget mWidget;
        private string mWidgetID;

        public TwitchModule()
        {
            mIRC = new TwitchIRC();
            mWidget = new ChatWidget(mIRC);
            mWidgetID = WidgetManager.Instance.Register(mWidget, "TEST-CHAT-WIDGET");
            Logger.Info("Registered Chat widget at link http://localhost:5000/widget/{0}", mWidgetID);
        }

        // TEMPORARY
        public void JoinChannel(string channel)
        {
            mIRC.JoinChannel(channel);
        }

        // TEMPORARY
        public void AddCommandToChannel(string channel, string commandName, Command.ICommand command)
        {
            mIRC.AddCommandToChannel(channel, commandName, command);
        }

        // TEMPORARY
        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }


        // IModule overrides

        public void Init()
        {
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            mIRC.RequestShutdown();
            mWidget.RequestShutdown();
        }

        public void Wait()
        {
            mIRC.WaitForShutdown();
            mWidget.WaitForShutdown();
        }
    }
}
