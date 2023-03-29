using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Widget;
using LukeBot.Widget.Common;
using LukeBot.Globals;
using LukeBot.Spotify;


namespace LukeBot
{
    class UserContext
    {
        public string Username { get; private set; }

        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";

        private readonly Dictionary<string, Func<string, IModule>> mModuleAllocators = new Dictionary<string, Func<string, IModule>>
        {
            {"twitch", (string user) => GlobalModules.Twitch.JoinChannel(user)},
            {"spotify", (string user) => new SpotifyModule(user)},
            {"widget", (string user) => GlobalModules.Widget.LoadWidgetUserModule(user)},
        };

        private List<IModule> mModules = null;

        private class ModuleDesc
        {
            public string moduleType { get; set; }
        }

        public UserContext(string user)
        {
            Username = user;
            mModules = new List<IModule>();

            Logger.Log().Info("Loading required modules for user {0}", Username);

            string[] usedModules;
            if (!Conf.TryGet<string[]>(
                Common.Utils.FormConfName(Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_MODULES_DOMAIN),
                out usedModules
            ))
            {
                usedModules = new string[0];
            }

            foreach (string m in usedModules)
            {
                LoadModule(m, Username);
            }

            Logger.Log().Info("Created LukeBot user {0}", Username);
        }

        private void LoadModule(string moduleType, string lbUser)
        {
            try
            {
                AddModule(mModuleAllocators[moduleType](lbUser));
            }
            catch (Common.Exception e)
            {
                Logger.Log().Error(String.Format("Failed to initialize module {0} for user {1}: {2}",
                    moduleType, Username, e.Message));
                Logger.Log().Error(String.Format("Module {0} for user {1} will be skipped",
                    moduleType, Username));
            }
        }

        // Create a new Module associated with this User.
        public void CreateModule(string moduleType)
        {
            // TODO
        }

        private void AddModule(IModule module)
        {
            mModules.Add(module);
        }

        public void RunModules()
        {
            Logger.Log().Info("Running LukeBot modules for user {0}", Username);
            foreach (IModule m in mModules)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IModule m in mModules)
                m.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IModule m in mModules)
                m.WaitForShutdown();
        }
    }
}
