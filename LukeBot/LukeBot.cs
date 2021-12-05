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
            Logger.Log().Info("Requested shutdown");
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

            Logger.Log().Info("LukeBot v0.0.1 starting");
            mCLI = new CLI.Interface();

            Logger.Log().Info("Initializing Core systems...");
            Core.Systems.Initialize();

            Logger.Log().Info("LukeBot UI starting...");
            mInterfaceThread.Start();

            Logger.Log().Info("LukeBot modules starting...");
            mUsers.Add(new UserContext("Lookey"));

            try
            {
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
                twitch.AddCommandToChannel("lookey", "attempts", new Twitch.Command.Print(
                    "I have some goals on my way to completing a hardcore \"run\". For more details and attempt history: https://docs.google.com/spreadsheets/d/1e2Hm9SVCaZkIG-wnFc5n-5RYcZtVJE0XOhcFUx0ZQNs/edit?usp=sharing"
                ));
                twitch.AddCommandToChannel("lookey", "mods", new Twitch.Command.Print(
                    "Fabric mods I'm using: Iris, Litematica, Logical Zoom, ModMenu, MiniHUD, Sodium, Tweakeroo; Shaders (if enabled): Sildur's Vibrant Shaders Extreme-VL"
                ));

                Logger.Log().Info("Giving control to CLI");
                mCLI.MainLoop();
            }
            catch (Common.Exception e)
            {
                e.Print(LogLevel.Error);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
            }

            mUI.Stop();
            mInterfaceThread.Join();

            Logger.Log().Info("UI stopped. Stopping modules...");
            mUsers[0].RequestModuleShutdown();
            mUsers[0].WaitForModulesShutdown();
            mUsers = null;

            Logger.Log().Info("Core systems teardown...");
            Core.Systems.Teardown();
        }
    }
}
