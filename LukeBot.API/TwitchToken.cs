using LukeBot.Common;

namespace LukeBot.API
{
    public class TwitchToken: Token
    {
        public TwitchToken(AuthFlow flow, string tokenId)
            : base(
                Constants.TWITCH_SERVICE_NAME,
                tokenId,
                flow,
                "https://id.twitch.tv/oauth2/authorize",
                "https://id.twitch.tv/oauth2/token",
                "https://id.twitch.tv/oauth2/revoke",
                "https://" + Utils.GetConfigServerIP() + "/callback/twitch"
            )
        {
        }

        ~TwitchToken()
        {
        }
    };
}
