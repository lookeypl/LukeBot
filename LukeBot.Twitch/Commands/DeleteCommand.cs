using System;
using LukeBot.Common;
using LukeBot.Twitch.Common;
using LukeBot.Communication;
using Command = LukeBot.Twitch.Common.Command;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Twitch.Command
{
    public class DeleteCommand: ICommand
    {
        private string mLBUser;

        public DeleteCommand(Command::Descriptor d, string lbUser)
            : base(d)
        {
            mLBUser = lbUser;
        }

        public override void Edit(string newValue)
        {
            // noop
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            if (args.Length < 2)
            {
                return "Not enough parameters - provide command name to delete";
            }

            DeleteCommandIntercomMsg msg = new DeleteCommandIntercomMsg();
            msg.User = mLBUser;
            msg.Name = args[1];

            Intercom::ResponseBase resp = Comms.Intercom.Request<Intercom::ResponseBase, DeleteCommandIntercomMsg>(msg);

            // we don't want to hang the bot for longer than 1 second (this is all internal communications
            // anyway so it shouldn't take long)
            resp.Wait(1000);

            if (resp.Status == Intercom::MessageStatus.SUCCESS)
            {
                return String.Format("Deleted {0} command successfully", msg.Name);
            }
            else
            {
                Logger.Log().Warning("Failed to delete command {0} for user {1} via chat: {2}", msg.Name, mLBUser, resp.ErrorReason);
                return String.Format("Failed to delete command {0}", msg.Name);
            }
        }

        public override Command::Descriptor ToDescriptor()
        {
            return new Command::Descriptor(mName, Command::Type.delcom, mPrivilegeLevel, "");
        }
    }
}