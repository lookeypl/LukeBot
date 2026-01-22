using System;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.User;


namespace LukeBot.Twitch.Impl.Command
{
    public class EditCommand: ICommand
    {
        public string mLBUser;

        private ITwitchUserModule GetUserModule()
        {
            IUserContext userContext = Service.Get<IUserService>().GetUser(mLBUser);
            return Service.Get<ITwitchService>().GetModule(userContext) as ITwitchUserModule;
        }

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

            string cmdName = args[1];

            try
            {
                // TODO this can edit other commands than just print commands - prevent from doing that
                // ex. add a parameter stating which type of command we want to delete/edit.
                GetUserModule().EditChatCommand(args[1], String.Join(' ', args, 2, args.Length - 2));
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to edit command {0} for user {1} via chat: {2}", cmdName, mLBUser, e.Message);
                return String.Format("Failed to edit command {0}", cmdName);
            }

            return String.Format("Edited {0} command successfully", cmdName);
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.editcom, mPrivilegeLevel, mEnabled, "");
        }
    }
}