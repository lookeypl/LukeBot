using LukeBot.Common;

namespace LukeBot.Twitch
{
    public class TwitchToken: Common.OAuth.Token
    {
        public TwitchToken(Common.OAuth.AuthFlow flow)
            : base(
                Constants.SERVICE_NAME,
                flow,
                "https://id.twitch.tv/oauth2/authorize",
                "https://id.twitch.tv/oauth2/token",
                "https://id.twitch.tv/oauth2/revoke",
                "http://localhost:5000/callback/twitch"
            )
        {
        }

        ~TwitchToken()
        {
        }
    };
}
