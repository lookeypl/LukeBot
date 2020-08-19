using LukeBot.Common;
using LukeBot.Twitch;
using System.Collections.Generic;
using System.Linq;

namespace LukeBot
{
    class LukeBot
    {
        private List<UserContext> mUsers;

        public LukeBot()
        {
            mUsers = new List<UserContext>();
        }

        public void Run()
        {
            Logger.Info("LukeBot v0.0.1 starting");

            mUsers.Add(new UserContext("Lookey"));

            mUsers[0].AddModule(new TwitchIRC());
            mUsers[0].RunModules();
        }
    }
}
