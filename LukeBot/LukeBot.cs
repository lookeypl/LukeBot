using LukeBot.Interface;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Endpoint;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Communication;
using LukeBot.User;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using LukeBot.Communication.Impl;
using LukeBot.Spotify.Impl;
using LukeBot.Twitch.Impl;
using LukeBot.User.Impl;
using LukeBot.Widget.Impl;


namespace LukeBot
{
    internal class LukeBot
    {
        private List<ICLIProcessor> mCommandProcessors = new List<ICLIProcessor> {
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

        private HostEndpoint mEndpoint = new();


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
            Service.Teardown();

            Logger.Log().Info("Stopping web endpoint...");
            mEndpoint.Stop();

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
                mEndpoint.Start();

                Logger.Log().Info("Initializing Services...");
                Service.Register(IntermediaryService.Create());
                Service.Register(UserService.Create());
                Service.Register(TwitchService.Create());
                Service.Register(SpotifyService.Create());
                Service.Register(WidgetService.Create());

                InterfaceType uiType = opts.CLI;
                Logger.Log().Info("Initializing UI {0}...", uiType.ToString());
                UserInterface.Initialize(uiType);

                Logger.Log().Info("Running Services...");
                Service.Run();

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
