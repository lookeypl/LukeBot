using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Twitch.Common.Command;
using Command = LukeBot.Twitch.Common.Command;
using CommandLine;


namespace LukeBot
{
    [Verb("addcom", HelpText = "Add command for user")]
    public class TwitchAddcomCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to add")]
        public string Name { get; set; }

        [Value(1, MetaName = "type", Required = true, HelpText = "Type of command to add")]
        public Command::Type Type { get; set; }

        [Value(2, MetaName = "value", Required = false, HelpText = "Value for the command")]
        public IEnumerable<string> Value { get; set; }

        public TwitchAddcomCommand()
        {
            Name = "";
            Type = Command::Type.print;
        }
    }

    [Verb("delcom", HelpText = "Delete command for user")]
    public class TwitchDelcomCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        public TwitchDelcomCommand()
        {
            Name = "";
        }
    }

    [Verb("editcom", HelpText = "Edit existing Twitch command")]
    public class TwitchEditcomCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        [Value(1, MetaName = "new_value", Required = true, HelpText = "New value to set to command")]
        public IEnumerable<string> Value { get; set; }

        public TwitchEditcomCommand()
        {
            Name = "";
        }
    }

    [Verb("listcom", HelpText = "List available Twitch commands for given user")]
    public class TwitchListcomCommand
    {
        public TwitchListcomCommand()
        {
        }
    }

    [Verb("modcom", HelpText = "Modify command's parameters")]
    public class TwitchModcomCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to modify")]
        public string Name { get; set; }

        [Option('a', "allow", SetName = "allow", HelpText = "Allow selected users to execute command")]
        public string Allowed { get; set; }

        [Option('d', "deny", SetName = "deny", HelpText = "Deny selected users to execute command")]
        public string Denied { get; set; }

        [Option('l', "list", SetName = "list", HelpText = "List current command modifiers")]
        public bool List { get; set; }

        public TwitchModcomCommand()
        {
        }
    }

    [Verb("enable", HelpText = "Enable Twitch module")]
    public class TwitchEnableCommand
    {
    }

    [Verb("disable", HelpText = "Disable Twitch module")]
    public class TwitchDisableCommand
    {
    }

    public class TwitchCommandProcessor: ICommandProcessor
    {
        private bool ValidateSelectedUser(out string lbUser)
        {
            lbUser = GlobalModules.CLI.GetSelectedUser();
            return (lbUser.Length > 0);
        }

        public void HandleAddcom(TwitchAddcomCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                Twitch.Command.ICommand twCmd = GlobalModules.Twitch.AllocateCommand(lbUser, cmd.Name, cmd.Type, string.Join(' ', cmd.Value));
                if (twCmd == null)
                {
                    msg = "Invalid command type";
                    return;
                }

                GlobalModules.Twitch.AddCommandToChannel(lbUser, cmd.Name, twCmd);
            }
            catch (System.Exception e)
            {
                msg = "Failed to add command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Added " + cmd.Type + " command " + cmd.Name + " for user " + lbUser;
        }

        public void HandleDelcom(TwitchDelcomCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                GlobalModules.Twitch.DeleteCommandFromChannel(lbUser, cmd.Name);
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Command " + cmd.Name + " for user " + lbUser + " deleted.";
        }

        public void HandleEditcom(TwitchEditcomCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                GlobalModules.Twitch.EditCommandFromChannel(lbUser, cmd.Name, string.Join(' ', cmd.Value));
            }
            catch (System.Exception e)
            {
                msg = "Failed to edit command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Edited twitch command " + cmd.Name + " for user " + lbUser;
        }

        public void HandleListcom(TwitchListcomCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            // TODO
            msg = "Available commands:\n";
        }

        public void HandleModcom(TwitchModcomCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                if (cmd.List)
                {
                    Command::Descriptor d = GlobalModules.Twitch.GetCommandDescriptor(lbUser, cmd.Name);

                    msg = "Modifiers of Twitch command " + cmd.Name + ":\n";
                    msg += "  Privileges: " + d.Privilege.GetStringRepresentation();
                    return;
                }
                else if (cmd.Allowed != null && cmd.Allowed.Length > 0)
                {
                    Command::User priv = cmd.Allowed.ToUserEnum();
                    if (priv == 0)
                    {
                        msg = "Invalid privilege list: " + cmd.Allowed;
                        return;
                    }

                    GlobalModules.Twitch.AllowPrivilegeInCommand(lbUser, cmd.Name, priv);
                    msg = "Command " + cmd.Name + " modified";
                    return;
                }
                else if (cmd.Denied != null && cmd.Denied.Length > 0)
                {
                    Command::User priv = cmd.Denied.ToUserEnum();
                    if (priv == 0)
                    {
                        msg = "Invalid privilege list: " + cmd.Allowed;
                        return;
                    }

                    GlobalModules.Twitch.DenyPrivilegeInCommand(lbUser, cmd.Name, priv);
                    msg = "Command " + cmd.Name + " modified";
                    return;
                }
                else
                {
                    msg = "No action provided";
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed - " + e.Message;
            }
        }

        public void HandleParseError(IEnumerable<Error> errs, out string msg)
        {
            msg = "Error while parsing twitch command";
        }

        public void AddCLICommands()
        {
            GlobalModules.CLI.AddCommand("twitch", (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<TwitchAddcomCommand, TwitchDelcomCommand, TwitchEditcomCommand,
                        TwitchListcomCommand, TwitchModcomCommand>(args)
                    .WithParsed<TwitchAddcomCommand>((TwitchAddcomCommand arg) => HandleAddcom(arg, out result))
                    .WithParsed<TwitchDelcomCommand>((TwitchDelcomCommand arg) => HandleDelcom(arg, out result))
                    .WithParsed<TwitchEditcomCommand>((TwitchEditcomCommand arg) => HandleEditcom(arg, out result))
                    .WithParsed<TwitchListcomCommand>((TwitchListcomCommand arg) => HandleListcom(arg, out result))
                    .WithParsed<TwitchModcomCommand>((TwitchModcomCommand arg) => HandleModcom(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => HandleParseError(errs, out result));
                return result;
            });
        }
    }
}