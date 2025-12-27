using System;
using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Services;
using LukeBot.Interface;
using LukeBot.Twitch.Common;
using LukeBot.User;
using CommandLine;


namespace LukeBot
{
    [Verb("command", HelpText = "Interact with Twitch Chat commands")]
    public class TwitchCommandSubverb
    {
    }

    [Verb("emote-refresh", HelpText = "Refreshes user's emotes. Use to reload emotes from third party providers (ex. FFZ) after adding new ones.")]
    public class TwitchEmoteRefreshSubverb
    {
    }

    [Verb("eventsub-restart", HelpText = "Restarts EventSub thread for current user. Useful if the thread happens to die for some reason")]
    public class TwitchEventSubRestartSubverb
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

        private ITwitchService GetTwitchService()
        {
            return Service.Get(Common.Constants.TWITCH_SERVICE_NAME) as ITwitchService;
        }

        private ITwitchUserModule GetTwitchUserModule(IUserContext user)
        {
            return GetTwitchService().GetModule(user) as ITwitchUserModule;
        }

        private void CheckForLogin(CLIMessageProxy CLI)
        {
            Path path = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(CLI.GetCurrentUser().GetUsername())
                .Push(Constants.TWITCH_SERVICE_NAME)
                .Push(Constants.PROP_STORE_LOGIN_PROP);

            if (!Conf.TryGet<string>(path, out string login))
            {
                login = CLI.Query(false, "Twitch login for user " + CLI.GetCurrentUser());
                if (login.Length == 0)
                {
                    throw new ArgumentException("No login provided");
                }

                Conf.Add(path, Property.Create<string>(login));
            }
        }

        private void HandleCommandSubverb(TwitchCommandSubverb arg, CLIMessageProxy CLI, string[] args, out string result)
        {
            result = mCommandCLIProcessor.Parse(CLI, args);
        }

        private void HandleEmoteRefreshSubverb(CLIMessageProxy CLI, out string result)
        {
            try
            {
                GetTwitchUserModule(CLI.GetCurrentUser()).RefreshEmotes();
                result = "Emotes refreshed";
            }
            catch (System.Exception e)
            {
                result = "Failed to refresh emotes: " + e.Message;
            }
        }

        private void HandleEventSubRestartSubverb(CLIMessageProxy CLI, out string result)
        {
            try
            {
                GetTwitchUserModule(CLI.GetCurrentUser()).RestartEventSub();

                result = "EventSub restarted";
            }
            catch (System.Exception e)
            {
                result = "Failed to restart EventSub thread: " + e.Message;
            }
        }

        private void HandleLoginSubverb(TwitchLoginSubverb arg, CLIMessageProxy CLI, string[] args, out string result)
        {
            result = "";

            if (args.Length != 1)
            {
                result = "Too many arguments - provide one argument being your Twitch login";
                return;
            }

            try
            {
                GetTwitchUserModule(CLI.GetCurrentUser()).UpdateLogin(args[0]);
                result = "Successfully updated Twitch login.";
            }
            catch (System.Exception e)
            {
                result = "Failed to update Twitch login: " + e.Message;
            }
        }

        public void HandleEnableSubverb(CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                CheckForLogin(CLI);
                GetTwitchService().CreateModule(CLI.GetCurrentUser());
                msg = "Enabled module " + Constants.TWITCH_SERVICE_NAME;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable Twitch module: " + e.Message;
            }
        }

        public void HandleDisableSubverb(CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                GetTwitchService().DestroyModule(CLI.GetCurrentUser());
                msg = "Disabled module " + Constants.TWITCH_SERVICE_NAME;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable Twitch module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;
            mCommandCLIProcessor = new TwitchCommandCLIProcessor(mLukeBot);

            UserInterface.CLI.AddCommand(Constants.TWITCH_SERVICE_NAME, PermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                string[] cmdArgs = args.Take(2).ToArray(); // filters out any additional options/commands that might confuse CommandLine
                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<TwitchCommandSubverb, TwitchEmoteRefreshSubverb, TwitchEventSubRestartSubverb, TwitchLoginSubverb, TwitchEnableSubverb, TwitchDisableSubverb>(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEmoteRefreshSubverb>((TwitchEmoteRefreshSubverb arg) => HandleEmoteRefreshSubverb(cliProxy, out result))
                    .WithParsed<TwitchEventSubRestartSubverb>((TwitchEventSubRestartSubverb arg) => HandleEventSubRestartSubverb(cliProxy, out result))
                    .WithParsed<TwitchLoginSubverb>((TwitchLoginSubverb arg) => HandleLoginSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(cliProxy, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.TWITCH_SERVICE_NAME, out result));
                return result;
            });
        }
    }
}