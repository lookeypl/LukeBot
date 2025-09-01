using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Services;
using LukeBot.Interface;
using LukeBot.User.Common;
using LukeBot.Widget.Common;
using CommandLine;
using LukeBot.Logging;


namespace LukeBot
{
    public class WidgetBaseCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID. Can be either UUID or its name.")]
        public string Id { get; set; }

        public WidgetBaseCommand()
        {
            Id = "";
        }
    }

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

    [Verb("list", HelpText = "List available widgets")]
    public class WidgetListCommand
    {
    }

    [Verb("address", HelpText = "Get widget's address")]
    public class WidgetAddressCommand: WidgetBaseCommand
    {
        public WidgetAddressCommand()
        {
        }
    }

    [Verb("info", HelpText = "Get more info on widget")]
    public class WidgetInfoCommand: WidgetBaseCommand
    {
        public WidgetInfoCommand()
        {
        }
    }

    [Verb("delete", HelpText = "Delete widget")]
    public class WidgetDeleteCommand: WidgetBaseCommand
    {
        public WidgetDeleteCommand()
        {
        }
    }

    [Verb("reload", HelpText = "Reload all widgets or selected widget.")]
    public class WidgetReloadCommand
    {
        [Value(0, MetaName = "id", Required = false, HelpText = "Widget's ID, can be either UUID or its name. Omit to reload all.")]
        public string Id { get; set; }

        [Option("recreate-config", Default = false, HelpText =
            "Recreates widget's configuration from scratch, in case something goes wrong when loading. Works only when Widget's ID is provided."
        )]
        public bool RecreateConfig { get; set; }

        public WidgetReloadCommand()
        {
        }
    }

    [Verb("config", HelpText = "Launches Widget Configuration editor.")]
    public class WidgetConfigCommand: WidgetBaseCommand
    {
    }

    [Verb("enable", HelpText = "Enable Widget support for current user.")]
    public class WidgetEnableCommand
    {
    }

    [Verb("disable", HelpText = "Enable Widget support for current user.")]
    public class WidgetDisableCommand
    {
    }


    internal class WidgetCLIProcessor: ICLIProcessor
    {
        private LukeBot mLukeBot;

        private IWidgetService GetWidgetService()
        {
            return Service.Get(Common.Constants.WIDGET_SERVICE_NAME) as IWidgetService;
        }

        private IWidgetUserModule GetWidgetUserModule(IUserContext user)
        {
            return GetWidgetService().GetModule(user) as IWidgetUserModule;
        }

        public void HandleAddCommand(WidgetAddCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            string addr;
            try
            {
                addr = GetWidgetUserModule(CLI.GetCurrentUser()).AddWidget(cmd.Type, cmd.Name);

                msg = "Added new widget at address: " + addr;
            }
            catch (System.Exception e)
            {
                msg = "Failed to add widget: " + e.Message;
            }
        }

        public void HandleAddressCommand(WidgetAddressCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            WidgetDesc wd;

            try
            {
                wd = GetWidgetUserModule(CLI.GetCurrentUser()).GetWidgetInfo(cmd.Id);

                msg = wd.Address;
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget's address: " + e.Message;
            }
        }

        public void HandleListCommand(WidgetListCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            IEnumerable<WidgetDesc> widgets;

            try
            {
                widgets = GetWidgetUserModule(CLI.GetCurrentUser()).ListWidgets();

                msg = "Available widgets:";
                foreach (WidgetDesc w in widgets)
                {
                    msg += "\n  " + w.Id + " (";

                    if (w.Name.Length > 0)
                        msg += w.Name + ", ";
                    msg += w.Type.ToString();

                    if (!GetWidgetUserModule(CLI.GetCurrentUser()).IsWidgetLoaded(w.Id))
                        msg += ", unloaded)";
                    else
                        msg += ")";
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to list widgets: " + e.Message;
            }
        }

        public void HandleInfoCommand(WidgetInfoCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            WidgetDesc wd;
            ConfigurationBase conf;

            try
            {
                wd = GetWidgetUserModule(CLI.GetCurrentUser()).GetWidgetInfo(cmd.Id);
                conf = GetWidgetUserModule(CLI.GetCurrentUser()).GetWidgetConfiguration(cmd.Id);
                Dictionary<string, ConfigurationField> fields = conf.GetFields();

                msg = "Widget " + cmd.Id + " info:\n" + wd.ToFormattedString();
                msg += "\nConfiguration:";
                if (fields.Count == 0)
                {
                    msg += " empty";
                }
                else
                {
                    msg += "\n";
                    foreach (ConfigurationField field in fields.Values)
                    {
                        msg += "  " + field.Name + ": " + field.GetValueString() + "\n";
                    }
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget info: " + e.Message;
            }
        }

        public void HandleDeleteCommand(WidgetDeleteCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                GetWidgetUserModule(CLI.GetCurrentUser()).DeleteWidget(cmd.Id);

                msg = "Widget " + cmd.Id + " deleted.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete widget: " + e.Message;
            }
        }

        public void HandleReloadCommand(WidgetReloadCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                if (cmd.Id != null && cmd.Id.Length > 0)
                {
                    if (cmd.RecreateConfig)
                    {
                        GetWidgetUserModule(CLI.GetCurrentUser()).ResetConfiguration(cmd.Id);
                    }

                    GetWidgetUserModule(CLI.GetCurrentUser()).ReloadWidget(cmd.Id);
                    msg = "Widget " + cmd.Id + " reloaded.";
                }
                else
                {
                    IEnumerable<WidgetDesc> widgets = GetWidgetUserModule(CLI.GetCurrentUser()).ListWidgets();

                    foreach (WidgetDesc wd in widgets)
                    {
                        GetWidgetUserModule(CLI.GetCurrentUser()).ReloadWidget(wd.Id);
                    }

                    msg = "Widgets reloaded.";
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to reload widget: " + e.Message;
            }
        }

        public void HandleConfigCommand(WidgetConfigCommand arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                ConfigurationBase config = GetWidgetUserModule(CLI.GetCurrentUser()).GetWidgetConfiguration(arg.Id);

                CLI.Message("Starting Widget Configuration Editor...");
                new WidgetConfigurationCLIEditor(arg.Id, config, CLI).MainLoop();

                GetWidgetUserModule(CLI.GetCurrentUser()).SaveConfiguration(arg.Id);
                msg = "Widget Configuration Editor closed.";
            }
            catch (System.Exception e)
            {
                msg = "Widget Configuration Editor error: " + e.Message;
                Logger.Log().Error("Widget Configuration Editor error: {0}", e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        public void HandleEnableCommand(WidgetEnableCommand arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                GetWidgetService().CreateModule(CLI.GetCurrentUser());
                msg = "Created module " + Constants.WIDGET_SERVICE_NAME;
            }
            catch (System.Exception e)
            {
                msg = "Failed to create Widget module: " + e.Message;
            }
        }

        public void HandleDisableCommand(WidgetDisableCommand arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                GetWidgetService().DestroyModule(CLI.GetCurrentUser());
                msg = "Destroyed module " + Constants.WIDGET_SERVICE_NAME;
            }
            catch (System.Exception e)
            {
                msg = "Failed to destroy Widget module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(Constants.WIDGET_SERVICE_NAME, PermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<WidgetAddCommand, WidgetAddressCommand, WidgetListCommand, WidgetInfoCommand, WidgetDeleteCommand,
                        WidgetReloadCommand, WidgetConfigCommand, WidgetEnableCommand, WidgetDisableCommand>(args)
                    .WithParsed<WidgetAddCommand>((WidgetAddCommand arg) => HandleAddCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetAddressCommand>((WidgetAddressCommand arg) => HandleAddressCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetListCommand>((WidgetListCommand arg) => HandleListCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetInfoCommand>((WidgetInfoCommand arg) => HandleInfoCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetDeleteCommand>((WidgetDeleteCommand arg) => HandleDeleteCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetReloadCommand>((WidgetReloadCommand arg) => HandleReloadCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetConfigCommand>((WidgetConfigCommand arg) => HandleConfigCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetEnableCommand>((WidgetEnableCommand arg) => HandleEnableCommand(arg, cliProxy, out result))
                    .WithParsed<WidgetDisableCommand>((WidgetDisableCommand arg) => HandleDisableCommand(arg, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.WIDGET_SERVICE_NAME, out result));
                return result;
            });
        }
    }
}
