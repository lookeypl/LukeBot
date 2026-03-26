using System;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.User;


namespace LukeBot.Twitch.Impl.Command
{
    public class DeleteCommand: ICommand
    {
        private IUserContext mLBUser;

        private ITwitchUserModule GetUserModule()
        {
            return Service.Get<ITwitchService>().GetModule(mLBUser) as ITwitchUserModule;
        }

        public DeleteCommand(Descriptor d, IUserContext lbUser)
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
            if (args.Length < 2)
            {
                return "Not enough parameters - provide command name to delete";
            }

            string cmdName = args[1];

            try
            {
                // TODO this can delete other commands than just print commands - prevent from doing that
                // ex. add a parameter stating which type of command we want to delete/edit.
                GetUserModule().DeleteChatCommand(cmdName);
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to delete command {0} for user {1} via chat: {2}", cmdName, mLBUser.GetUsername(), e.Message);
                return String.Format("Failed to delete command {0}", cmdName);
            }

            return String.Format("Deleted {0} command successfully", cmdName);
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.delcom, mPrivilegeLevel, mEnabled, "");
        }
    }
}