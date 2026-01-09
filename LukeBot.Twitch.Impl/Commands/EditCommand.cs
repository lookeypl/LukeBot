using System;
using LukeBot.Logging;
using LukeBot.Communication.Impl;
using LukeBot.Twitch.Command;
using Intercom = LukeBot.Communication.Intercom;


namespace LukeBot.Twitch.Impl.Command
{
    public class EditCommand: ICommand
    {
        public string mLBUser;

        public EditCommand(Descriptor d, string lbUser)
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
                return "Not enough parameters - provide command name and new message to print";
            }

            EditCommandIntercomMsg msg = new EditCommandIntercomMsg();
            msg.lbUser = mLBUser;
            msg.Name = args[1];
            msg.Param = String.Join(' ', args, 2, args.Length - 2);

            Intercom::ResponseBase resp = Comms.Intercom.Request<Intercom::ResponseBase, EditCommandIntercomMsg>(msg);

            // we don't want to hang the bot for longer than 1 second (this is all internal communications
            // anyway so it shouldn't take long)
            resp.Wait(1000);

            if (resp.Status == Intercom::MessageStatus.SUCCESS)
            {
                return String.Format("Edited {0} command successfully", msg.Name);
            }
            else
            {
                Logger.Log().Warning("Failed to edit command {0} for user {1} via chat: {2}", msg.Name, mLBUser, resp.ErrorReason);
                return String.Format("Failed to edit command {0}", msg.Name);
            }
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.editcom, mPrivilegeLevel, mEnabled, "");
        }
    }
}