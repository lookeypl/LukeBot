using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Widget.Common;
using CommandLine;


namespace LukeBot
{
    [Verb("add", HelpText = "Add widget for user")]
    public class WidgetAddCommand
    {
        [Value(0, MetaName = "type", Required = true, HelpText = "Type of widget to add")]
        public WidgetType Type { get; set; }

        [Value(1, MetaName = "name", Default = "", Required = false, HelpText = "User-friendly name of widget")]
        public string Name { get; set; }

        public WidgetAddCommand()
        {
            Type = WidgetType.invalid;
            Name = "";
        }
    }

    [Verb("address", HelpText = "Get widget's address")]
    public class WidgetAddressCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetAddressCommand()
        {
            Id = "";
        }
    }

    [Verb("list", HelpText = "List available widgets")]
    public class WidgetListCommand
    {
    }

    [Verb("info", HelpText = "Get more info on widget")]
    public class WidgetInfoCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetInfoCommand()
        {
            Id = "";
        }
    }

    [Verb("delete", HelpText = "Delete widget")]
    public class WidgetDeleteCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetDeleteCommand()
        {
            Id = "";
        }
    }

    public class WidgetCLIProcessor: ICLIProcessor
    {
        private bool ValidateSelectedUser(out string lbUser)
        {
            lbUser = GlobalModules.CLI.GetSelectedUser();
            return (lbUser.Length > 0);
        }

        public void HandleAdd(WidgetAddCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            string addr;
            try
            {
                addr = GlobalModules.Widget.AddWidget(user, cmd.Type, cmd.Name);
            }
            catch (System.Exception e)
            {
                msg = "Failed to add widget: " + e.Message;
                return;
            }

            msg = "Added new widget at address: " + addr;
        }

        public void HandleAddress(WidgetAddressCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            WidgetDesc wd;

            try
            {
                wd = GlobalModules.Widget.GetWidgetInfo(user, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget's address: " + e.Message;
                return;
            }

            msg = wd.Address;
        }

        public void HandleList(WidgetListCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            List<WidgetDesc> widgets;

            try
            {
                widgets = GlobalModules.Widget.ListUserWidgets(user);
            }
            catch (System.Exception e)
            {
                msg = "Failed to list widgets: " + e.Message;
                return;
            }

            msg = "Available widgets:";
            foreach (WidgetDesc w in widgets)
            {
                msg += "\n  " + w.Id + " (";
                if (w.Name.Length > 0)
                    msg += w.Name + ", ";
                msg += w.Type.ToString() + ")";
            }
        }

        public void HandleInfo(WidgetInfoCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            WidgetDesc wd;

            try
            {
                wd = GlobalModules.Widget.GetWidgetInfo(user, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget info: " + e.Message;
                return;
            }

            msg = "Widget " + cmd.Id + " info:\n" + wd.ToFormattedString();
        }

        public void HandleDelete(WidgetDeleteCommand cmd, out string msg)
        {
            string user;
            if (!ValidateSelectedUser(out user))
            {
                msg = "User is not selected";
                return;
            }

            try
            {
                GlobalModules.Widget.DeleteWidget(user, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete widget: " + e.Message;
                return;
            }

            msg = "Widget " + cmd.Id + " deleted.";
        }

        public void AddCLICommands()
        {
            GlobalModules.CLI.AddCommand("widget", (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<WidgetAddCommand, WidgetAddressCommand, WidgetListCommand, WidgetInfoCommand, WidgetDeleteCommand>(args)
                    .WithParsed<WidgetAddCommand>((WidgetAddCommand arg) => HandleAdd(arg, out result))
                    .WithParsed<WidgetAddressCommand>((WidgetAddressCommand arg) => HandleAddress(arg, out result))
                    .WithParsed<WidgetListCommand>((WidgetListCommand arg) => HandleList(arg, out result))
                    .WithParsed<WidgetInfoCommand>((WidgetInfoCommand arg) => HandleInfo(arg, out result))
                    .WithParsed<WidgetDeleteCommand>((WidgetDeleteCommand arg) => HandleDelete(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, "widget", out result));
                return result;
            });
        }
    }
}
