using System;

namespace SpotifyManager
{
    class MainClass
    {
        static void Main(string[] args)
        {
            SpotifyManager mgr = new SpotifyManager();
            mgr.Run(args);
        }
    }
}
