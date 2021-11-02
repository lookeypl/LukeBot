using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Twitch;

namespace LukeBot
{
    class UserContext
    {
        private string mUser = null;
        private List<IModule> mModules = null;

        public UserContext(string user)
        {
            mUser = user;
            mModules = new List<IModule>();
            Logger.Log().Info("Registered LukeBot user " + mUser);
        }

        public void AddModule(IModule module)
        {
            mModules.Add(module);
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
        }

        public void WaitForModulesShutdown()
        {
            foreach (IModule m in mModules)
                m.WaitForShutdown();
        }
    }
}
