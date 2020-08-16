using LukeBot.Common;
using LukeBot.Twitch;

namespace LukeBot
{

class LukeBot
{
    private TwitchIRC mTwitch = null;
    private string mTwitchBotAccount = "lukeboto";
    private string mTwitchBotChannel = "lookey";
    private string mTwitchOAuthFile = "Data/oauth_secret.lukebot";

    public void Run()
    {
        Logger.Info("LukeBot v0.0.1 starting");

        mTwitch = new TwitchIRC(mTwitchBotAccount, mTwitchBotChannel, mTwitchOAuthFile);
        mTwitch.Connect();
        mTwitch.Run();
    }
}

}
