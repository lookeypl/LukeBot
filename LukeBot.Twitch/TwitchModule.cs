using LukeBot.Common;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        TwitchIRC mIRC;
        ChatWidget mWidget;

        public TwitchModule()
        {
            mIRC = new TwitchIRC();
            mWidget = new ChatWidget(mIRC);
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

        public void WaitForShutdown()
        {
            mIRC.WaitForShutdown();
            mWidget.WaitForShutdown();
        }
    }
}
