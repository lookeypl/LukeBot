using System.Collections.Generic;
using LukeBot.Common;
using Widget = LukeBot.Widget;

namespace LukeBot
{
    class UserContext
    {
        private string mUser = null;
        private List<IModule> mModules = null;
        private Widget::Manager mWidgets = null;

        public UserContext(string user)
        {
            mUser = user;
            mModules = new List<IModule>();
            mWidgets = new Widget::Manager();
            Logger.Log().Info("Registered LukeBot user " + mUser);
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
            Logger.Log().Info("Initializing LukeBot modules");
            foreach (IModule m in mModules)
                m.Init();

            Logger.Log().Info("Running LukeBot modules");
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
