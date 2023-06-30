using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public class Print: ICommand
    {
        private string mMessage = "";

        public Print(Command::Descriptor d)
            : base(d)
        {
            mMessage = d.Value;
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            return mMessage;
        }

        public override void Edit(string newValue)
        {
            mMessage = newValue;
        }

        public override Command::Descriptor ToDescriptor()
        {
            return new Command::Descriptor(mName, Command::Type.print, mPrivilegeLevel, mMessage);
        }
    }
}