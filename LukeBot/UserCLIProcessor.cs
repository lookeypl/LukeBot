using System.Collections.Generic;
using LukeBot.Services;
using LukeBot.Interface;
using LukeBot.User.Common;
using CommandLine;

namespace LukeBot
{
    [Verb("add", HelpText = "Add user")]
    internal class UserCreateCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to add")]
        public string Name { get; set; }

        public UserCreateCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available users")]
    internal class UserListCommand
    {
    }

    [Verb("remove", HelpText = "Remove user")]
    internal class UserRemoveCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to remove")]
        public string Name { get; set; }

        public UserRemoveCommand()
        {
            Name = "";
        }
    }

    [Verb("switch", HelpText = "Switch to a different user for further commands")]
    internal class UserSwitchCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to select. Leave empty to deselect.")]
        public string Name { get; set; }

        public UserSwitchCommand()
        {
            Name = "";
        }
    }

    [Verb("password", HelpText = "Set a password for current or selected user")]
    internal class UserPasswordCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to change password for")]
        public string Name { get; set; }

        public UserPasswordCommand()
        {
            Name = "";
        }
    }

    [Verb("update", HelpText = "Update user profile settings.")]
    internal class UserUpdateCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to change password for. Can be omitted to affect selected user.")]
        public string Name { get; set; }

        [Option('p', "permission", SetName = "permission", HelpText =
            "Set permission level for user. Available levels:\n" +
            "  - User\n" +
            "  - Admin\n")]
        public PermissionLevel PermissionLevel { get; set; }

        public UserUpdateCommand()
        {
            Name = "";
            PermissionLevel = PermissionLevel.None;
        }
    }

    internal class UserCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "user";
        private LukeBot mLukeBot;
        private CLIMessageProxy mCLI;

        private IUserService GetUserService()
        {
            return Service.Get(Common.Constants.USER_SERVICE_NAME) as IUserService;
        }

        private void HandleCreateUserCommand(UserCreateCommand args, out string msg)
        {
            try
            {
                GetUserService().CreateNewUser(args.Name);
                msg = "User " + args.Name + " added successfully";
            }
            catch (System.Exception e)
            {
                msg = "Failed to add user " + args.Name + ": " + e.Message;
            }
        }

        private void HandleListUsersCommand(UserListCommand args, out string msg)
        {
            msg = "Available users:\n";

            List<string> usernames = GetUserService().GetUsernames();
            foreach (string u in usernames)
            {
                msg += "  " + u + " (" + GetUserService().GetUser(u).GetPermissionLevel().ToString() + ")\n";
            }
        }

        private void HandleRemoveUserCommand(UserRemoveCommand args, out string msg)
        {
            if (!mCLI.Ask("Are you sure you want to remove user " + args.Name + "? This will remove all associated data!"))
            {
                msg = "User removal aborted";
                return;
            }

            try
            {
                try
                {
                    if (mCLI.GetCurrentUser().GetUsername() == args.Name)
                        mCLI.SetCurrentUser(null);
                }
                catch (NoUserSelectedException)
                {
                    // noop
                }

                GetUserService().RemoveUser(args.Name);
                msg = "User " + args.Name + " removed.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to remove user " + args.Name + ": " + e.Message;
            }
        }

        private void HandleSwitchUserCommand(UserSwitchCommand args, out string msg)
        {
            try
            {
                bool hasUsername = (args.Name != null && args.Name.Length > 0);
                if (hasUsername && !GetUserService().IsUsernameValid(args.Name))
                    throw new System.ArgumentException("Unknown/invalid username.");

                if (args.Name.Length == 0)
                    mCLI.SetCurrentUser(null); // unset current user
                else
                    mCLI.SetCurrentUser(GetUserService().GetUser(args.Name));

                try
                {
                    msg = "Switched to user " + mCLI.GetCurrentUser().GetUsername();
                }
                catch (NoUserSelectedException)
                {
                    msg = "Cleared current user";
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to switch to user " + args.Name + ": " + e.Message;
            }
        }

        private void HandlePasswordUserCommand(UserPasswordCommand args, out string msg)
        {
            try
            {
                IUserContext user;
                bool currentUser = (args.Name == null || args.Name.Length == 0);
                if (currentUser)
                    user = mCLI.GetCurrentUser();
                else
                    user = GetUserService().GetUser(args.Name);

                string newPwd = mCLI.Query(true, "New password");
                string newPwdRepeat = mCLI.Query(true, "Repeat new password");

                if (newPwd != newPwdRepeat)
                {
                    msg = "New passwords do not match";
                    return;
                }

                user.SetPasswordLocal(newPwd);
                msg = "Password changed";
            }
            catch (System.Exception e)
            {
                msg = "Failed to change password: " + e.Message;
            }
        }

        private void HandleUpdateUserCommand(UserUpdateCommand args, out string msg)
        {
            try
            {
                IUserContext user;
                bool currentUser = (args.Name == null || args.Name.Length == 0);
                if (currentUser)
                    user = mCLI.GetCurrentUser();
                else
                    user = GetUserService().GetUser(args.Name);

                user.SetPermissionLevel(args.PermissionLevel);

                if (currentUser)
                    mCLI.RefreshUserData();

                mCLI.Message("Permission level set to " + args.PermissionLevel.ToString());

                msg = "Changes to user " + user.GetUsername() + " applied.";
                msg = "TODO";
            }
            catch (System.Exception e)
            {
                msg = "Failed to update user: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, PermissionLevel.Admin, (CLIMessageProxy cliProxy, string[] args) =>
            {
                mCLI = cliProxy;

                string result = "";
                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<UserCreateCommand, UserListCommand, UserRemoveCommand, UserSwitchCommand, UserPasswordCommand, UserUpdateCommand>(args)
                    .WithParsed<UserCreateCommand>((UserCreateCommand args) => HandleCreateUserCommand(args, out result))
                    .WithParsed<UserListCommand>((UserListCommand args) => HandleListUsersCommand(args, out result))
                    .WithParsed<UserRemoveCommand>((UserRemoveCommand args) => HandleRemoveUserCommand(args, out result))
                    .WithParsed<UserSwitchCommand>((UserSwitchCommand args) => HandleSwitchUserCommand(args, out result))
                    .WithParsed<UserPasswordCommand>((UserPasswordCommand args) => HandlePasswordUserCommand(args, out result))
                    .WithParsed<UserUpdateCommand>((UserUpdateCommand args) => HandleUpdateUserCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}