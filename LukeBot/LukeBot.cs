using LukeBot.Common;
using LukeBot.Twitch;
using System.Collections.Generic;
using System.Linq;

namespace LukeBot
{
    class LukeBot
    {
        private List<IModule> mModules = null;

        void StartModules()
        {
            Logger.Info("Initializing LukeBot modules");
            foreach (IModule m in mModules)
                m.Init();

            Logger.Info("Running LukeBot modules");
            foreach (IModule m in mModules)
                m.Run();
        }

        public LukeBot()
        {
            mModules = new List<IModule>();
        }

        public void Run()
        {
            Logger.Info("LukeBot v0.0.1 starting");

            mModules.Add(new TwitchIRC());

            StartModules();
        }
    }
}
