using System;
using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Services;
using LukeBot.Interface;
using LukeBot.Twitch;
using LukeBot.User;
using CommandLine;
using LukeBot.Twitch.Impl;


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

    [Verb("chatters", HelpText = "Prints out the list of known chatters. List can be limited in size.")]
    public class TwitchChattersSubverb
    {
    }

    [Verb("chatter", HelpText = "Prints out information -known by LukeBot- about specified chatter. Note that this does NOT fetch information from Twitch, just checks what the bot managed to locally cache.")]
    public class TwitchChatterSubverb
    {
        [Value(0, MetaName = "chatterName", Required = true, HelpText = "Username of chatter to fetch information of.")]
        public string ChatterName { get; set; }
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
            return Service.Get<ITwitchService>();
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
                login = CLI.Query(false, "Twitch login for user " + CLI.GetCurrentUser().GetUsername());
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

        private void HandleChattersSubverb(TwitchChattersSubverb arg, CLIMessageProxy CLI, out string result)
        {
            result = "";

            try
            {
                IEnumerable<string> chatters = GetTwitchUserModule(CLI.GetCurrentUser()).GetKnownChatUsers();

                string chatterList = "";
                int perLine = 5;
                int lineCtr = 0;
                foreach (string chatter in chatters)
                {
                    chatterList += chatter + ", ";
                    lineCtr++;

                    if (lineCtr >= perLine)
                    {
                        chatterList += '\n';
                        lineCtr = 0;
                    }
                }

                result = "Known chatters list:\n\n" + chatterList + "\n";
            }
            catch (System.Exception e)
            {
                result = "Failed to fetch current list of chatters: " + e.Message;
            }
        }

        private void HandleChatterSubverb(TwitchChatterSubverb arg, CLIMessageProxy CLI, out string result)
        {
            result = "";

            try
            {
                Twitch.Chatter chatter = GetTwitchUserModule(CLI.GetCurrentUser()).GetChatter(arg.ChatterName);

                result += chatter.Username;
                if (chatter.DisplayName != chatter.Username)
                {
                    result += " (" + chatter.DisplayName + ")\n";
                }
                else
                {
                    result += "\n";
                }

                result += "Color: " + chatter.Color + "\n";
                result += "Badges:\n";
                foreach (string badge in chatter.Badges)
                {
                    result += "  - " + badge + "\n";
                }
                result += "\n";
            }
            catch (System.Exception e)
            {
                result = "Failed to fetch chatter " + arg.ChatterName + ": " + e.Message;
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
                p.ParseArguments<
                    TwitchCommandSubverb,
                    TwitchEmoteRefreshSubverb,
                    TwitchEventSubRestartSubverb,
                    TwitchLoginSubverb,
                    TwitchChattersSubverb,
                    TwitchChatterSubverb,
                    TwitchEnableSubverb,
                    TwitchDisableSubverb
                >(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEmoteRefreshSubverb>((TwitchEmoteRefreshSubverb arg) => HandleEmoteRefreshSubverb(cliProxy, out result))
                    .WithParsed<TwitchEventSubRestartSubverb>((TwitchEventSubRestartSubverb arg) => HandleEventSubRestartSubverb(cliProxy, out result))
                    .WithParsed<TwitchLoginSubverb>((TwitchLoginSubverb arg) => HandleLoginSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchChattersSubverb>((TwitchChattersSubverb arg) => HandleChattersSubverb(arg, cliProxy, out result))
                    .WithParsed<TwitchChatterSubverb>((TwitchChatterSubverb arg) => HandleChatterSubverb(arg, cliProxy, out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(cliProxy, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.TWITCH_SERVICE_NAME, out result));
                return result;
            });
        }
    }
}