using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Module;
using CommandLine;


namespace LukeBot
{
    [Verb("command", HelpText = "Interact with Twitch Chat commands")]
    public class TwitchCommandSubverb
    {
    }

    [Verb("login", HelpText = "Set login to Twitch servers. This will invalidate current auth token if it exists.")]
    public class TwitchLoginSubverb
    {
    }

    [Verb("enable", HelpText = "Enable Twitch module")]
    public class TwitchEnableSubverb
    {
    }

    [Verb("disable", HelpText = "Disable Twitch module")]
    public class TwitchDisableSubverb
    {
    }

    internal class TwitchCLIProcessor: ICLIProcessor
    {
        private TwitchCommandCLIProcessor mCommandCLIProcessor;
        private LukeBot mLukeBot;

        private void HandleCommandSubverb(TwitchCommandSubverb arg, string[] args, out string result)
        {
            result = mCommandCLIProcessor.Parse(args);
        }

        private void HandleLoginSubverb(TwitchLoginSubverb arg, string[] args, out string result)
        {
            result = "";

            if (args.Length != 1)
            {
                result = "Too many arguments - provide one argument being your Twitch login";
                return;
            }

            try
            {
                GlobalModules.Twitch.UpdateLoginForUser(mLukeBot.GetCurrentUser().Username, args[0]);
                result = "Successfully updated Twitch login.";
            }
            catch (System.Exception e)
            {
                result = "Failed to update Twitch login: " + e.Message;
            }
        }

        public void HandleEnableSubverb(TwitchEnableSubverb arg, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetCurrentUser().EnableModule(ModuleType.Twitch);
                msg = "Enabled module " + ModuleType.Twitch;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable Twitch module: " + e.Message;
            }
        }

        public void HandleDisableSubverb(TwitchDisableSubverb arg, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetCurrentUser().DisableModule(ModuleType.Twitch);
                msg = "Disabled module " + ModuleType.Twitch;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable Twitch module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mCommandCLIProcessor = new TwitchCommandCLIProcessor();
            mLukeBot = lb;

            CLI.Instance.AddCommand(Constants.TWITCH_MODULE_NAME, (string[] args) =>
            {
                string result = "";
                string[] cmdArgs = args.Take(2).ToArray(); // filters out any additional options/commands that might confuse CommandLine
                Parser.Default.ParseArguments<TwitchCommandSubverb, TwitchLoginSubverb, TwitchEnableSubverb, TwitchDisableSubverb>(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchLoginSubverb>((TwitchLoginSubverb arg) => HandleLoginSubverb(arg, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(arg, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.TWITCH_MODULE_NAME, out result));
                return result;
            });
        }
    }
}