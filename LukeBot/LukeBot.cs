using LukeBot.Common;
using LukeBot.Twitch;
using System.Collections.Generic;
using System.Linq;

namespace LukeBot
{
    class LukeBot
    {
        private List<IModule> mModules = null;
        private string mTwitchBotAccount = "lukeboto";
        private string mTwitchBotChannel = "lookey";
        private string mTwitchOAuthFile = "Data/oauth_secret.lukebot";

        public LukeBot()
        {
            mModules = new List<IModule>();
        }

        public void Run()
        {
            Logger.Info("LukeBot v0.0.1 starting");

            mModules.Add(new TwitchIRC(mTwitchBotAccount, mTwitchBotChannel, mTwitchOAuthFile));

            Logger.Info("Initializing LukeBot modules");
            foreach (IModule m in mModules)
                m.Init();

            Logger.Info("Running LukeBot modules");
            foreach (IModule m in mModules)
                m.Run();
        }
    }
}
