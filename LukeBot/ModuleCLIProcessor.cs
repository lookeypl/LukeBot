using System.Collections.Generic;
using LukeBot.Interface;
using LukeBot.Services;
using LukeBot.User.Common;
using CommandLine;

namespace LukeBot
{
    [Verb("list", HelpText = "List enabled modules for current user")]
    public class ModuleListCommand
    {
    }

    [Verb("enable", HelpText = "Enable a module for current user")]
    public class ModuleEnableCommand
    {
        [Value(0, MetaName = "type", Required = false, Default = "", HelpText = "Type of module to enable.")]
        public string Type { get; set; }

        public ModuleEnableCommand()
        {
            Type = "";
        }
    }
    [Verb("disable", HelpText = "Disable a module for current user")]
    public class ModuleDisableCommand
    {
        [Value(0, MetaName = "type", Required = false, Default = "", HelpText = "Type of module to disable.")]
        public string Type { get; set; }

        public ModuleDisableCommand()
        {
            Type = "";
        }
    }

    // LKTODO USER: reimplement
    internal class ModuleCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "module";
        private LukeBot mLukeBot;

        void HandleListCommand(ModuleListCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                msg = "Enabled modules:";

                List<string> modules = new(); //mLukeBot.GetUser(CLI.GetCurrentUser()).GetEnabledModules();
                foreach (string m in modules)
                {
                    msg += "\n  " + m;
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to get enabled modules: " + e.Message;
            }
        }

        void HandleEnableCommand(ModuleEnableCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                //IUserContext user = Service.User.GetUser(CLI.GetCurrentUser());
                //Service.ModuleManager.EnableModuleForUser(user, args.Type);
                //mLukeBot.GetUser(CLI.GetCurrentUser()).EnableModule(args.Type);
                msg = "Enabled module " + args.Type;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable module " + args.Type + ": " + e.Message;
            }
        }

        void HandleDisableCommand(ModuleDisableCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
               //mLukeBot.GetUser(CLI.GetCurrentUser()).DisableModule(args.Type);
                msg = "Disabled module " + args.Type;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable module " + args.Type + ": " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, PermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<ModuleListCommand, ModuleEnableCommand, ModuleDisableCommand>(args)
                    .WithParsed<ModuleListCommand>((ModuleListCommand args) => HandleListCommand(args, cliProxy, out result))
                    .WithParsed<ModuleEnableCommand>((ModuleEnableCommand args) => HandleEnableCommand(args, cliProxy, out result))
                    .WithParsed<ModuleDisableCommand>((ModuleDisableCommand args) => HandleDisableCommand(args, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}