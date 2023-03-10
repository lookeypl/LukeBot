using LukeBot.Twitch.Common;


namespace LukeBot.Twitch.Command
{
    public class Print: ICommand
    {
        private string mMessage = "";

        public Print(string name, string msg)
            : base(name)
        {
            mMessage = msg;
        }

        public override string Execute(string[] args)
        {
            return mMessage;
        }

        public override void Edit(string newValue)
        {
            mMessage = newValue;
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, TwitchCommandType.print, mMessage);
        }
    }
}