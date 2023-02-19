using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using Widget = LukeBot.Widget;
using LukeBot.Twitch;
using LukeBot.Spotify;


namespace LukeBot
{
    public class WidgetDesc
    {
        public string widgetType { get; set; }
        public string widgetID { get; set; }
    }

    class UserContext
    {
        public string Username { get; private set; }

        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";

        private readonly Dictionary<string, Func<string, IModule>> mModuleAllocators = new Dictionary<string, Func<string, IModule>>
        {
            {"twitch", (string user) => GlobalModules.Twitch.JoinChannel(user)},
            {"spotify", (string user) => new SpotifyModule(user)},
        };
        private readonly Dictionary<string, Func<Widget::IWidget>> mWidgetAllocators = new Dictionary<string, Func<Widget::IWidget>>
        {
            {"alerts", () => new Widget::Alerts()},
            {"chat", () => new Widget::Chat()},
            {"echo", () => new Widget::Echo()},
            {"nowplaying", () => new Widget::NowPlaying()},
        };

        private List<IModule> mModules = null;
        private Widget::Manager mWidgets = null;

        private class ModuleDesc
        {
            public string moduleType { get; set; }
        }

        public UserContext(string user)
        {
            Username = user;
            mModules = new List<IModule>();
            mWidgets = new Widget::Manager();

            Logger.Log().Info("Loading required modules for user {0}", Username);

            string[] usedModules = Conf.Get<string[]>(
                Common.Utils.FormConfName(Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_MODULES_DOMAIN)
            );

            WidgetDesc[] usedWidgets;
            try
            {
                usedWidgets = Conf.Get<WidgetDesc[]>(
                    Common.Utils.FormConfName(Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_WIDGETS_DOMAIN)
                );
            }
            catch (PropertyNotFoundException)
            {
                usedWidgets = new WidgetDesc[0];
            }

            foreach (string m in usedModules)
            {
                LoadModule(m, Username);
            }

            foreach (WidgetDesc w in usedWidgets)
            {
                LoadWidget(w);
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

        private string LoadWidget(WidgetDesc desc)
        {
            return AddWidget(mWidgetAllocators[desc.widgetType](), desc.widgetID);
        }

        // Create a new Module associated with this User.
        public void CreateModule(string moduleType)
        {
            // TODO
        }

        // Create a new Widget used by this user
        public void CreateWidget(string widgetType)
        {
            // TODO
        }

        private void AddModule(IModule module)
        {
            mModules.Add(module);
        }

        private string AddWidget(Widget::IWidget widget, string widgetID)
        {
            return mWidgets.Register(widget, widgetID);
        }

        public void RunModules()
        {
            mWidgets.Init();

            Logger.Log().Info("Running LukeBot modules for user {0}", Username);
            foreach (IModule m in mModules)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IModule m in mModules)
                m.RequestShutdown();

            mWidgets.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IModule m in mModules)
                m.WaitForShutdown();

            mWidgets.WaitForShutdown();
        }
    }
}
