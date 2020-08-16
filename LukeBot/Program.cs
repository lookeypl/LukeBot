using LukeBot.Common;
using System;
using System.IO;

namespace LukeBot
{

class Program
{
    static private TwitchIRC mTwitch = null;
    static private string mTwitchBotAccount = "lukeboto";
    static private string mTwitchBotChannel = "lookey";
    static private string mTwitchOAuthFile = "Data/oauth_secret.lukebot";

    static void Main(string[] args)
    {
        Logger.Info("LukeBot v0.0.1");

        FileUtils.SetUnifiedCWD();

        try
        {
            mTwitch = new TwitchIRC(mTwitchBotAccount, mTwitchBotChannel, mTwitchOAuthFile);
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
