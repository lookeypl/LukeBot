using System;
using LukeBot.Logging;
using LukeBot.Communication;
using LukeBot.Twitch.Command;
using Intercom = LukeBot.Communication.Common.Intercom;


namespace LukeBot.Twitch.Impl.Command
{
    public class AddCommand: ICommand
    {
        private string mLBUser;

        public AddCommand(Descriptor d, string lbUser)
            : base(d)
        {
            mLBUser = lbUser;
        }

        public override void Edit(string newValue)
        {
            // noop
        }

        public override string Execute(ChatUser callerPrivilege, string[] args)
        {
            if (args.Length < 3)
            {
                return "Not enough parameters - provide command name and message to print";
            }

            AddCommandIntercomMsg msg = new AddCommandIntercomMsg();
            msg.lbUser = mLBUser;
            msg.Name = args[1];
            msg.Type = CommandType.print; // we assume from chat-level you can only add print commands
            msg.Param = String.Join(' ', args, 2, args.Length - 2);

            Intercom::ResponseBase resp = Comms.Intercom.Request<Intercom::ResponseBase, AddCommandIntercomMsg>(msg);

            // we don't want to hang the bot for longer than 1 second (this is all internal communications
            // anyway so it shouldn't take long)
            resp.Wait(1000);

            if (resp.Status == Intercom::MessageStatus.SUCCESS)
            {
                return String.Format("Added {0} command successfully", msg.Name);
            }
            else
            {
                Logger.Log().Warning("Failed to add command {0} for user {1} via chat: {2}", msg.Name, mLBUser, resp.ErrorReason);
                return String.Format("Failed to add command {0}", msg.Name);
            }
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.addcom, mPrivilegeLevel, mEnabled, "");
        }
    }
}