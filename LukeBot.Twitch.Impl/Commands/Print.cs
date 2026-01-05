using LukeBot.Twitch.Command;


namespace LukeBot.Twitch.Impl.Command
{
    public class Print: ICommand
    {
        private string mMessage = "";

        public Print(Descriptor d)
            : base(d)
        {
            mMessage = d.Value;
        }

        public override string Execute(ChatUser callerPrivilege, string[] args)
        {
            return mMessage;
        }

        public override void Edit(string newValue)
        {
            mMessage = newValue;
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.print, mPrivilegeLevel, mEnabled, mMessage);
        }
    }
}