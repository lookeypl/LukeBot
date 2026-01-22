using System;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.User;


namespace LukeBot.Twitch.Impl.Command
{
    public class AddCommand: ICommand
    {
        private string mLBUser;

        private ITwitchUserModule GetUserModule()
        {
            IUserContext userContext = Service.Get<IUserService>().GetUser(mLBUser);
            return Service.Get<ITwitchService>().GetModule(userContext) as ITwitchUserModule;
        }

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

            Descriptor newCmdDesc = new(
                args[1],
                CommandType.print, // we assume from chat-level you can only add print commands
                String.Join(' ', args, 2, args.Length - 2)
            );

            try
            {
                GetUserModule().AddChatCommand(newCmdDesc);
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to add command {0} for user {1} via chat: {2}", newCmdDesc.Name, mLBUser, e.Message);
                return String.Format("Failed to add command {0}", newCmdDesc.Name);
            }

            return String.Format("Added {0} command successfully", newCmdDesc.Name);
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.addcom, mPrivilegeLevel, mEnabled, "");
        }
    }
}