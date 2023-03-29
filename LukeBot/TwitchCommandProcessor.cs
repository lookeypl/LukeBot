using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Twitch.Common;
using CommandLine;


namespace LukeBot
{
    [Verb("addcom", HelpText = "Add command for user")]
    public class TwitchAddcomCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to add")]
        public string Name { get; set; }

        [Value(1, MetaName = "type", Required = true, HelpText = "Type of command to add")]
        public TwitchCommandType Type { get; set; }

        [Value(2, MetaName = "value", Required = false, HelpText = "Value for the command")]
        public IEnumerable<string> Value { get; set; }

        public TwitchAddcomCommand()
        {
            Name = "";
            Type = TwitchCommandType.print;
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
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                Twitch.Command.ICommand twCmd = GlobalModules.Twitch.AllocateCommand(cmd.Name, cmd.Type, string.Join(' ', cmd.Value));
                if (twCmd == null)
                {
                    msg = "Invalid command type";
                    return;
                }

                GlobalModules.Twitch.AddCommandToChannel(user, cmd.Name, twCmd);
            }
            catch (System.Exception e)
            {
                msg = "Failed to add command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Added " + cmd.Type + " command " + cmd.Name + " for user " + user;
        }

        public void HandleDelcom(TwitchDelcomCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                GlobalModules.Twitch.DeleteCommandFromChannel(user, cmd.Name);
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Command " + cmd.Name + " for user " + user + " deleted.";
        }

        public void HandleEditcom(TwitchEditcomCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                GlobalModules.Twitch.EditCommandFromChannel(user, cmd.Name, string.Join(' ', cmd.Value));
            }
            catch (System.Exception e)
            {
                msg = "Failed to edit command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Edited twitch command " + cmd.Name + " for user " + user;
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
                Parser.Default.ParseArguments<TwitchAddcomCommand, TwitchDelcomCommand, TwitchEditcomCommand>(args)
                    .WithParsed<TwitchAddcomCommand>((TwitchAddcomCommand arg) => HandleAddcom(arg, out result))
                    .WithParsed<TwitchDelcomCommand>((TwitchDelcomCommand arg) => HandleDelcom(arg, out result))
                    .WithParsed<TwitchEditcomCommand>((TwitchEditcomCommand arg) => HandleEditcom(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => HandleParseError(errs, out result));
                return result;
            });
        }
    }
}