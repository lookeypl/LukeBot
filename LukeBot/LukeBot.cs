using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Twitch;
using LukeBot.Communication;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using CommandLine;


namespace LukeBot
{
    class LukeBot
    {
        private string DEVMODE_FILE = "Data/devmode.lukebot";

        private List<UserContext> mUsers;
        private List<ICommandProcessor> mCommandProcessors = new List<ICommandProcessor>{
            new TwitchCommandProcessor(),
            new WidgetCommandProcessor()
        };

        void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            // UI is not handled here; it captures Ctrl+C on its own
            Logger.Log().Info("Requested shutdown");
            mUsers[0].RequestModuleShutdown();
        }

        public LukeBot()
        {
            mUsers = new List<UserContext>();
        }

        ~LukeBot()
        {
        }

        void LoadUsers()
        {
            string[] users = Conf.Get<string[]>("lukebot.users");

            if (users.Length == 0)
            {
                Logger.Log().Info("No users found");
                return;
            }

            foreach (string user in users)
            {
                Logger.Log().Info("Loading LukeBot user " + user);
                mUsers.Add(new UserContext(user));
            }

            foreach (UserContext u in mUsers)
            {
                Logger.Log().Info("Running modules for User " + u.Username);
                u.RunModules();
            }
        }

        void UnloadUsers()
        {
            Logger.Log().Info("Unloading users...");

            foreach (UserContext u in mUsers)
            {
                u.RequestModuleShutdown();
            }

            foreach (UserContext u in mUsers)
            {
                u.WaitForModulesShutdown();
            }

            mUsers.Clear();
        }

        void AddUserToConfig(string name)
        {
            string[] users;

            string propName = Utils.FormConfName(
                Constants.PROP_STORE_LUKEBOT_DOMAIN, Constants.PROP_STORE_USERS_PROP
            );
            if (!Conf.TryGet<string[]>(propName, out users))
            {
                users = new string[1];
                users[0] = name;
                Conf.Add(propName, Property.Create<string[]>(users));
                return;
            }

            Array.Resize(ref users, users.Length + 1);
            users[users.Length - 1] = name;
            Conf.Modify<string[]>(propName, users);
        }

        void AddUser(UserAddCommand args, out string msg)
        {
            try
            {
                AddUserToConfig(args.Name);

                UserContext uc = new UserContext(args.Name);
                uc.RunModules();

                mUsers.Add(uc);

                msg = "User " + args.Name + " added successfully";
            }
            catch (System.Exception e)
            {
                msg = "Failed to add user " + args.Name + ": " + e.Message;
            }
        }

        void ListUsers(UserListCommand args, out string msg)
        {
            msg = "Available users:";

            foreach (UserContext uc in mUsers)
            {
                msg += "\n  " + uc.Username;
            }
        }

        void RemoveUser(UserRemoveCommand args, out string msg)
        {
            if (!GlobalModules.CLI.Ask("Are you sure you want to remove user " + args.Name + "? This will remove all associated data!"))
            {
                msg = "User removal aborted";
                return;
            }

            msg = "User " + args.Name + " removed.";
        }

        void SelectUser(UserSelectCommand args, out string msg)
        {
            if (args.Name.Length == 0)
            {
                // deselect user
                msg = "Deselected user " + GlobalModules.CLI.GetSelectedUser();
                GlobalModules.CLI.SaveSelectedUser("");
                return;
            }

            if (mUsers.Exists(ctx => ctx.Username == args.Name))
            {
                GlobalModules.CLI.SaveSelectedUser(args.Name);
                msg = "Selected user " + GlobalModules.CLI.GetSelectedUser();
            }
            else
            {
                msg = "User " + args.Name + " not found";
            }
        }

        void HandleParseError(IEnumerable<Error> errs, out string msg)
        {
            msg = "Parsing user subcommand failed:\n";
            foreach (Error e in errs)
            {
                if (e is HelpVerbRequestedError || e is HelpRequestedError)
                    continue;

                msg += e.Tag.ToString() + '\n';
            }
        }

        void AddCLICommands()
        {
            GlobalModules.CLI.AddCommand("user", (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<UserAddCommand, UserListCommand, UserRemoveCommand, UserSelectCommand>(args)
                    .WithParsed<UserAddCommand>((UserAddCommand args) => AddUser(args, out result))
                    .WithParsed<UserListCommand>((UserListCommand args) => ListUsers(args, out result))
                    .WithParsed<UserRemoveCommand>((UserRemoveCommand args) => RemoveUser(args, out result))
                    .WithParsed<UserSelectCommand>((UserSelectCommand args) => SelectUser(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => HandleParseError(errs, out result));
                return result;
            });

            foreach (ICommandProcessor cp in mCommandProcessors)
            {
                cp.AddCLICommands();
            }
        }

        public bool IsInDevMode()
        {
            try
            {
                if (FileUtils.Exists(DEVMODE_FILE))
                {
                    string data = File.ReadAllText(DEVMODE_FILE);
                    int enabled = 0;
                    if (!Int32.TryParse(data, out enabled))
                        return false;
                    return (enabled != 0);
                }
            }
            catch
            {
                // quietly exit
            }

            return false;
        }

        public void StartDevmode()
        {
            Logger.Log().Warning("ENABLED DEVELOPER MODE");

            /*Logger.Log().Info("Starting web endpoint...");
            Endpoint.Endpoint.StartThread();

            try
            {
                Logger.Log().Info("LukeBot modules starting...");
                UserContext devUser = new UserContext("Dev");
                mUsers.Add(devUser);

                TwitchMainModule twitch = new TwitchMainModule();
                mUsers[0].AddModule(twitch);
                mUsers[0].AddModule(new SpotifyModule());
                mUsers[0].RunModules();

                twitch.AwaitIRCLoggedIn(120 * 1000);
                twitch.JoinChannel("lookey");

                twitch.AddCommandToChannel("lookey", "so", new Twitch.Command.Shoutout());

                Widget.Echo echo = new Widget.Echo();
                devUser.AddWidget(echo, "TEST-ECHO");
                devUser.AddWidget(new Widget.Chat(), "TEST-CHAT-WIDGET");
                //devUser.AddWidget(new Widget.NowPlaying(), "BIG-NOW-PLAYING-WIDGET");
                devUser.AddWidget(new Widget.NowPlaying(), "SMALL-NOW-PLAYING-WIDGET");
                devUser.AddWidget(new Widget.Alerts(), "TEST-ALERTS");

                Logger.Log().Info("Giving control to CLI");
                mCLI.MainLoop();
            }
            catch (Common.Exception e)
            {
                e.Print(LogLevel.Error);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
                Logger.Log().Error("Backtrace:\n{0}", e.StackTrace);
            }

            Logger.Log().Info("Stopping modules...");
            mUsers[0].RequestModuleShutdown();
            mUsers[0].WaitForModulesShutdown();
            mUsers = null;

            Logger.Log().Info("Stopping web endpoint...");
            Endpoint.Endpoint.StopThread();*/
        }

        public void Run(ProgramOptions opts)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

/*
            if (IsInDevMode())
            {
                StartDevmode();
                Logger.Log().Info("Core systems teardown...");
                Core.Comms.Teardown();
                Conf.Teardown();
                return;
            }

            Logger.Log().Info("LukeBot modules starting...");
            mUsers.Add(new UserContext("Lookey"));

            try
            {
                TwitchModule twitch = new TwitchModule();
                mUsers[0].AddModule(twitch);
                mUsers[0].AddModule(new SpotifyModule());
                mUsers[0].RunModules();

                twitch.AwaitIRCLoggedIn(120 * 1000);
                twitch.JoinChannel("lookey");
                twitch.AddCommandToChannel("lookey", "!so", new Twitch.Command.Shoutout());
                twitch.AddCommandToChannel("lookey", "!discord", new Twitch.Command.Print("Discord server: https://discord.gg/wsx2sY5"));
                twitch.AddCommandToChannel("lookey", "!spoilers", new Twitch.Command.Print(
                    "No spoilers please! To prevent random people from spoiling the game chat is in 10 minute Followers-only mode during Playthroughs. That will stay on at least until we're done with main story of the game. VeryPog"
                ));
                twitch.AddCommandToChannel("lookey", "!cam3", new Twitch.Command.Print(
                    "I like it"
                ));
                twitch.AddCommandToChannel("lookey", "!hunt", new Twitch.Command.Print(
                    "I'm trying to get all ATs in the current campaign! I'm doing it WITHOUT prior knowledge related to the campaign (don't know ATs/replays for maps + figure out the route and improvements on my own) so please respect it and don't spoil or backseat - otherwise you might be timed out/banned."
                ));
                twitch.AddCommandToChannel("lookey", "!attempts", new Twitch.Command.Print(
                    "I have some goals on my way to completing a hardcore \"run\". For more details and attempt history: https://docs.google.com/spreadsheets/d/1e2Hm9SVCaZkIG-wnFc5n-5RYcZtVJE0XOhcFUx0ZQNs/edit?usp=sharing"
                ));
                twitch.AddCommandToChannel("lookey", "!mcmods", new Twitch.Command.Print(
                    "Fabric mods I'm using: Iris, Litematica, Logical Zoom, ModMenu, MiniHUD, Sodium, Tweakeroo; Shaders (if enabled): Sildur's Vibrant Shaders Extreme-VL"
                ));
                twitch.AddCommandToChannel("lookey", "!rando", new Twitch.Command.Print(
                    "Randomizer I'm using is MFOR (Metroid Fusion Open Randomizer) v 0.9.7; you can get it here: https://forum.metroidconstruction.com/index.php/topic,5376.0.html"
                ));

                mUsers[0].AddWidget(new Widget.Chat(), "TEST-CHAT-WIDGET");
                mUsers[0].AddWidget(new Widget.NowPlaying(), "BIG-NOW-PLAYING-WIDGET");
                mUsers[0].AddWidget(new Widget.NowPlaying(), "SMALL-NOW-PLAYING-WIDGET");

                Logger.Log().Info("Giving control to CLI");
                mCLI.MainLoop();
            }
            catch (Common.Exception e)
            {
                e.Print(LogLevel.Error);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
                Logger.Log().Error("Backtrace:\n{0}", e.StackTrace);
            }

            Logger.Log().Info("Stopping modules...");
            mUsers[0].RequestModuleShutdown();
            mUsers[0].WaitForModulesShutdown();
            mUsers = null;
*/
            try
            {
                Logger.Log().Info("LukeBot v0.0.1 starting");

                Logger.Log().Info("Loading configuration...");
                Conf.Initialize(opts.StoreDir);

                Logger.Log().Info("Initializing Core Comms...");
                Comms.Initialize();

                Logger.Log().Info("Starting web endpoint...");
                Endpoint.Endpoint.StartThread();

                Logger.Log().Info("Initializing Global Modules...");
                GlobalModules.Initialize();

                GlobalModules.Run();

                LoadUsers();

                // We'll get stuck here until the end
                Logger.Log().Info("Giving control to CLI");
                AddCLICommands();
                GlobalModules.CLI.MainLoop();
            }
            catch (Common.Exception e)
            {
                e.Print(LogLevel.Error);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
                Logger.Log().Error("Backtrace:\n{0}", e.StackTrace);
            }

            UnloadUsers();

            Logger.Log().Info("Stopping Global Modules...");
            GlobalModules.Stop();

            Logger.Log().Info("Stopping web endpoint...");
            Endpoint.Endpoint.StopThread();

            Logger.Log().Info("Core systems teardown...");
            GlobalModules.Teardown();
            Comms.Teardown();
            Conf.Teardown();
        }
    }
}
