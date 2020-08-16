using LukeBot.Common;
using System;

namespace LukeBot
{

class Program
{
    static private TwitchIRC mTwitch = null;
    static private string mTwitchPass = "TOPSECRET";

    static void Main(string[] args)
    {
        Logger.Info("LukeBot v0.0.1");

        try
        {
            mTwitch = new TwitchIRC("lukeboto", "lookey", mTwitchPass);
            mTwitch.Connect();
            mTwitch.Run();
        }
        catch (Exception e)
        {
            Logger.Error("Caught exception: " + e.Message + "\n" + e.StackTrace);
        }
    }
}

}
