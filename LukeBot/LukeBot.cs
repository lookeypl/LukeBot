using LukeBot.Interface;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.User.Common;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;


namespace LukeBot
{
    internal class LukeBot
    {

        private List<ICLIProcessor> mCommandProcessors = new List<ICLIProcessor>{
            new EventCLIProcessor(),
            new ModuleCLIProcessor(),
            new TestCLIProcessor(),
            new UserCLIProcessor(),

            // module CLI commands
            // TODO this should be added by modules themselves on Main Module initialization
            new SpotifyCLIProcessor(),
            new TwitchCLIProcessor(),
            new WidgetCLIProcessor(),
        };

        public LukeBot()
        {
        }

        ~LukeBot()
        {
        }

        public void OpenBrowserURLCallback(object o, EventArgsBase args)
        {
            // hooks up to AuthManager's OpenBrowserURL
            API.OpenBrowserURLArgs a = args as API.OpenBrowserURLArgs;
            UserInterface.CLI.OpenBrowserURL(a.LukeBotUser, a.URL);
        }

        private void AddCLICommands()
        {
            foreach (ICLIProcessor cp in mCommandProcessors)
            {
                cp.AddCLICommands(this);
            }
        }

        private void Shutdown()
        {
            UserInterface.Teardown();

            Logger.Log().Info("Stopping Services...");
            Service.Stop();

            Logger.Log().Info("Stopping web endpoint...");
            Endpoint.Endpoint.StopThread();

            Logger.Log().Info("Core systems teardown...");
            Service.Teardown();
            Comms.Teardown();
            Conf.Teardown();
        }

        public void Run(ProgramOptions opts)
        {
            try
            {
                Logger.Log().Info("LukeBot v0.0.1 starting");

                Logger.Log().Info("Loading configuration...");
                Conf.Initialize(opts.StoreDir);

                Logger.Log().Info("Initializing Core Comms...");
                Comms.Initialize();

                // TODO hacky??? maybe it could be done better
                API.AuthManager i = API.AuthManager.Instance; // triggers constructor and initializes below event's endpoint
                Comms.Event.Global().Event(API.Events.AUTHMGR_OPEN_BROWSER).Endpoint += OpenBrowserURLCallback;

                Logger.Log().Info("Starting web endpoint...");
                Endpoint.Endpoint.StartThread();

                Logger.Log().Info("Initializing Services...");
                Service.Initialize();

                InterfaceType uiType = opts.CLI;
                Logger.Log().Info("Initializing UI {0}...", uiType.ToString());
                UserInterface.Initialize(uiType);

                Logger.Log().Info("Running Services...");
                Service.Run();

                Logger.Log().Info("Loading Users...");
                Service.User.LoadUsers();

                Logger.Log().Info("Giving control to UI");
                AddCLICommands();
                UserInterface.CLI.MainLoop();
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

            Shutdown();
        }
    }
}
