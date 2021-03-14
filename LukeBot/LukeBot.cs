using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.UI;
using System.Collections.Generic;
using System.Threading;
using System;

namespace LukeBot
{
    class LukeBot
    {
        private List<UserContext> mUsers;
        private Thread mInterfaceThread;

        void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            Logger.Info("Requested shutdown");
            mUsers[0].RequestModuleShutdown();
        }

        public LukeBot()
        {
            mUsers = new List<UserContext>();
            mInterfaceThread = new Thread(new ThreadStart(Interface.ThreadMain));
        }

        ~LukeBot()
        {
            if (mInterfaceThread != null)
                mInterfaceThread.Join();
        }

        public void Run(string[] args)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

            Logger.Info("LukeBot v0.0.1 starting");

            Logger.Info("LukeBot interface starting...");
            mInterfaceThread.Start();

            Logger.Info("LukeBot modules starting...");
            mUsers.Add(new UserContext("Lookey"));

            TwitchIRC twitch = new TwitchIRC();
            mUsers[0].AddModule(twitch);
            mUsers[0].RunModules();

            twitch.AwaitLoggedIn(30 * 1000);
            twitch.JoinChannel("lookey");
            twitch.AddCommandToChannel("lookey", "discord", new Twitch.Command.Print("Discord server: https://discord.gg/wsx2sY5"));

            mUsers[0].WaitForModules();
        }
    }
}
