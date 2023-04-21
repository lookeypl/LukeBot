using System.Collections.Generic;
using System.Linq;
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

    public class TwitchCLIProcessor: ICLIProcessor
    {
        private TwitchCommandCLIProcessor mCommandCLIProcessor;

        private void HandleCommandSubverb(TwitchCommandSubverb arg, string[] args, out string result)
        {
            result = mCommandCLIProcessor.Parse(args);
        }

        private void HandleEnableSubverb(TwitchEnableSubverb arg, out string result)
        {
            // TODO
            result = "";
        }

        private void HandleDisableSubverb(TwitchDisableSubverb arg, out string result)
        {
            // TODO
            result = "";
        }

        public void AddCLICommands()
        {
            mCommandCLIProcessor = new TwitchCommandCLIProcessor();

            GlobalModules.CLI.AddCommand("twitch", (string[] args) =>
            {
                string result = "";
                string[] cmdArgs = args.Take(2).ToArray(); // filters out any additional options/commands that might confuse CommandLine
                Parser.Default.ParseArguments<TwitchCommandSubverb, TwitchEnableSubverb, TwitchDisableSubverb>(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(arg, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, "twitch", out result));
                return result;
            });
        }
    }
}