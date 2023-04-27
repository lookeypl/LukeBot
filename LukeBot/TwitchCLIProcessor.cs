using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Globals;
using CommandLine;


namespace LukeBot
{
    [Verb("command", HelpText = "Interact with Twitch Chat commands")]
    public class TwitchCommandSubverb
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

        public void HandleEnableSubverb(TwitchEnableSubverb arg, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetCurrentUser().EnableModule(Constants.TWITCH_MODULE_NAME);
                msg = "Enabled module " + Constants.TWITCH_MODULE_NAME;
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
                mLukeBot.GetCurrentUser().DisableModule(Constants.TWITCH_MODULE_NAME);
                msg = "Disabled module " + Constants.TWITCH_MODULE_NAME;
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

            GlobalModules.CLI.AddCommand(Constants.TWITCH_MODULE_NAME, (string[] args) =>
            {
                string result = "";
                string[] cmdArgs = args.Take(2).ToArray(); // filters out any additional options/commands that might confuse CommandLine
                Parser.Default.ParseArguments<TwitchCommandSubverb, TwitchEnableSubverb, TwitchDisableSubverb>(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(arg, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.TWITCH_MODULE_NAME, out result));
                return result;
            });
        }
    }
}