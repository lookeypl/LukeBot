using System;
using System.Linq;
using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Twitch.Common.Command;
using Command = LukeBot.Twitch.Common.Command;
using CommandLine;


namespace LukeBot
{
    [Verb("add", HelpText = "Add command for user")]
    public class TwitchAddCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to add.")]
        public string Name { get; set; }

        [Value(1, MetaName = "type", Required = true, HelpText =
            "Type of command to add. Available command types: " +
            "  print, shoutout, addcom, editcom, delcom"
        )]
        public Command::Type Type { get; set; }

        [Value(2, MetaName = "value", Required = false, HelpText = "Value for the command.")]
        public IEnumerable<string> Value { get; set; }

        public TwitchAddCommand()
        {
            Name = "";
            Type = Command::Type.print;
        }
    }

    [Verb("delete", HelpText = "Delete command for user")]
    public class TwitchDeleteCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        public TwitchDeleteCommand()
        {
            Name = "";
        }
    }

    [Verb("edit", HelpText = "Edit existing Twitch command")]
    public class TwitchEditCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        [Value(1, MetaName = "new_value", Required = true, HelpText = "New value to set to command")]
        public IEnumerable<string> Value { get; set; }

        public TwitchEditCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available Twitch commands for selected user")]
    public class TwitchListCommand
    {
        public TwitchListCommand()
        {
        }
    }

    [Verb("modify", HelpText = "Modify command's parameters")]
    public class TwitchModifyCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to modify")]
        public string Name { get; set; }

        [Option('a', "allow", SetName = "allow", HelpText =
            "Allow selected users to execute command. Available user groups are:\n" +
            "  Broadcaster, Moderator, VIP, Subscriber, Chatter\n" +
            "\n" +
            "Above groups must be provided in a comma-separated list, case-agnostic, without spaces.\n" +
            "Command also accepts \"Everyone\" (case-agnostic) as an alias of all groups above at once.\n" +
            "\n" +
            "It is possible to provide only first letters of a group.\n"
        )]
        public string Allowed { get; set; }

        [Option('d', "deny", SetName = "deny", HelpText =
            "Deny selected users to execute command. Available user groups are:\n" +
            "  Broadcaster, Moderator, VIP, Subscriber, Chatter\n" +
            "\n" +
            "Above groups must be provided in a comma-separated list, case-agnostic, without spaces.\n" +
            "Command also accepts \"Everyone\" (case-agnostic) as an alias of all groups above at once.\n" +
            "\n" +
            "It is possible to provide only first letters of a group.\n"
        )]
        public string Denied { get; set; }

        [Option('l', "list", SetName = "list", HelpText = "List current command modifiers")]
        public bool List { get; set; }

        public TwitchModifyCommand()
        {
        }
    }

    public class TwitchCommandCLIProcessor
    {
        private bool ValidateSelectedUser(out string lbUser)
        {
            lbUser = GlobalModules.CLI.GetSelectedUser();
            return (lbUser.Length > 0);
        }

        public void HandleAddcom(TwitchAddCommand cmd, out string msg)
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

        public void HandleDelcom(TwitchDeleteCommand cmd, out string msg)
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

        public void HandleEditcom(TwitchEditCommand cmd, out string msg)
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

        public void HandleListcom(TwitchListCommand cmd, out string msg)
        {
            string lbUser;
            if (!ValidateSelectedUser(out lbUser))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                msg = "Available commands:\n";

                List<Command::Descriptor> cmds = GlobalModules.Twitch.GetCommandDescriptors(lbUser);

                foreach (Command::Descriptor c in cmds)
                {
                    msg += String.Format("  {0} ({1})\n", c.Name, c.Type.ToString());
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to fetch available commands: " + e.Message;
            }
        }

        public void HandleModcom(TwitchModifyCommand cmd, out string msg)
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

        public string Parse(string[] args)
        {
            string result = "";
            Parser.Default.ParseArguments<TwitchAddCommand, TwitchDeleteCommand, TwitchEditCommand,
                    TwitchListCommand, TwitchModifyCommand>(args)
                .WithParsed<TwitchAddCommand>((TwitchAddCommand arg) => HandleAddcom(arg, out result))
                .WithParsed<TwitchDeleteCommand>((TwitchDeleteCommand arg) => HandleDelcom(arg, out result))
                .WithParsed<TwitchEditCommand>((TwitchEditCommand arg) => HandleEditcom(arg, out result))
                .WithParsed<TwitchListCommand>((TwitchListCommand arg) => HandleListcom(arg, out result))
                .WithParsed<TwitchModifyCommand>((TwitchModifyCommand arg) => HandleModcom(arg, out result))
                .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, "twitch-command", out result));
            return result;
        }
    }
}