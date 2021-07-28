using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.Spotify;
using LukeBot.UI;
using LukeBot.CLI;
using System.Collections.Generic;
using System.Threading;
using System;

namespace LukeBot
{
    class LukeBot
    {
        private List<UserContext> mUsers;
        private Thread mInterfaceThread;
        private CLI.Interface mCLI;
        private UI.Interface mUI;

        void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            // UI is not handled here; it captures Ctrl+C on its own
            Logger.Info("Requested shutdown");
            mCLI.Terminate();
            mUsers[0].RequestModuleShutdown();
        }

        public LukeBot()
        {
            mUsers = new List<UserContext>();
            mCLI = new CLI.Interface();
            mUI = new UI.Interface();
            mInterfaceThread = new Thread(new ThreadStart(mUI.Run));
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
            mCLI = new CLI.Interface();

            Logger.Info("LukeBot UI starting...");
            mInterfaceThread.Start();

            Logger.Info("LukeBot modules starting...");
            mUsers.Add(new UserContext("Lookey"));

            TwitchModule twitch = new TwitchModule();
            mUsers[0].AddModule(twitch);
            mUsers[0].AddModule(new SpotifyModule());
            mUsers[0].RunModules();

            twitch.AwaitIRCLoggedIn(120 * 1000);
            twitch.JoinChannel("lookey");
            twitch.AddCommandToChannel("lookey", "discord", new Twitch.Command.Print("Discord server: https://discord.gg/wsx2sY5"));
            twitch.AddCommandToChannel("lookey", "spoilers", new Twitch.Command.Print(
                "No spoilers please! To prevent random people from spoiling the game chat is in 10 minute Followers-only mode during Playthroughs. That will stay on at least until we're done with main story of the game. VeryPog"
            ));
            twitch.AddCommandToChannel("lookey", "cam3", new Twitch.Command.Print(
                "I like it"
            ));
            twitch.AddCommandToChannel("lookey", "hunt", new Twitch.Command.Print(
                "I'm trying to get all ATs in the current campaign! I'm doing it WITHOUT prior knowledge related to the campaign (don't know ATs/replays for maps + figure out the route and improvements on my own) so please respect it and don't spoil or backseat - otherwise you might be timed out/banned."
            ));

            Logger.Info("Giving control to CLI");
            mCLI.MainLoop();

            mUI.Stop();
            mInterfaceThread.Join();

            Logger.Info("UI stopped. Stopping modules...");
            mUsers[0].RequestModuleShutdown();
            mUsers[0].WaitForModules();
            mUsers = null;
        }
    }
}
