using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using Widget = LukeBot.Widget;
using LukeBot.Twitch;
using LukeBot.Spotify;


namespace LukeBot
{
    class UserContext
    {
        private const string PROP_STORE_USER_DOMAIN = "user";
        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";

        private readonly Dictionary<string, Func<IModule>> mModuleAllocators = new Dictionary<string, Func<IModule>>();
        private readonly Dictionary<string, Func<Widget::IWidget>> mWidgetAllocators = new Dictionary<string, Func<Widget::IWidget>>();

        private string mUser = null;
        private List<IModule> mModules = null;
        private Widget::Manager mWidgets = null;

        private class ModuleDesc
        {
            public string moduleType { get; set; }
        }

        private class WidgetDesc
        {
            public string widgetType { get; set; }
            public string widgetID { get; set; }
        }

        public UserContext(string user)
        {
            mUser = user;
            mModules = new List<IModule>();
            mWidgets = new Widget::Manager();

            mModuleAllocators.Add("twitch", () => new TwitchModule());
            mModuleAllocators.Add("spotify", () => new SpotifyModule());

            mWidgetAllocators.Add("alerts", () => new Widget::Alerts());
            mWidgetAllocators.Add("chat", () => new Widget::Chat());
            mWidgetAllocators.Add("echo", () => new Widget::Echo());
            mWidgetAllocators.Add("nowplaying", () => new Widget::NowPlaying());

            Logger.Log().Info("Loading required modules for user {0}", mUser);

            string[] usedModules = Conf.Get<string[]>(
                Common.Utils.FormConfName(PROP_STORE_USER_DOMAIN, mUser, PROP_STORE_MODULES_DOMAIN)
            );
            WidgetDesc[] usedWidgets = Conf.Get<WidgetDesc[]>(
                Common.Utils.FormConfName(PROP_STORE_USER_DOMAIN, mUser, PROP_STORE_WIDGETS_DOMAIN)
            );

            foreach (string m in usedModules)
            {
                CreateModule(m);
            }

            foreach (WidgetDesc w in usedWidgets)
            {
                CreateWidget(w);
            }

            Logger.Log().Info("Created LukeBot user {0}", mUser);
        }

        private void CreateModule(string moduleType)
        {
            AddModule(mModuleAllocators[moduleType]());
        }

        private string CreateWidget(WidgetDesc desc)
        {
            return AddWidget(mWidgetAllocators[desc.widgetType](), desc.widgetID);
        }

        public void AddModule(IModule module)
        {
            mModules.Add(module);
        }

        public string AddWidget(Widget::IWidget widget, string widgetID)
        {
            return mWidgets.Register(widget, widgetID);
        }

        public void RunModules()
        {
            mWidgets.Init();

            Logger.Log().Info("Initializing LukeBot modules for user {0}", mUser);
            foreach (IModule m in mModules)
                m.Init();

            Logger.Log().Info("Running LukeBot modules for user {0}", mUser);
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
