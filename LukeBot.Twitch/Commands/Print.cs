using LukeBot.Common;


namespace LukeBot.Twitch.Command
{
    public class Print: ICommand
    {
        private string mMessage = "";

        public Print(string msg)
        {
            mMessage = msg;
        }

        public string Execute(string[] args)
        {
            return mMessage;
        }
    }
}