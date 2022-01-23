using LukeBot.Common;

namespace SpotifyManager
{
    class SpotifyManager
    {
        public void Run(string[] args)
        {
            Logger.Log().Info("SpotifyManager started");

            if (args.Length < 1)
            {
                Logger.Log().Error("Argument error - provide Spotify login ex. \"SpotifyManager.exe user\"");
                return;
            }

            Logger.Log().Info("SpotifyManager shutting down...");
        }
    }
}