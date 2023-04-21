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
        private List<ICLIProcessor> mCommandProcessors = new List<ICLIProcessor>{
            new TwitchCLIProcessor(),
            new WidgetCLIProcessor()
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

            foreach (ICLIProcessor cp in mCommandProcessors)
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

        public void Run(ProgramOptions opts)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

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
