using LukeBot.Common;


namespace LukeBot.API
{
    public class TwitchToken: Token
    {
        public TwitchToken(AuthFlow flow, string lbUser)
            : base(
                Constants.TWITCH_SERVICE_NAME,
                lbUser,
                flow,
                "https://id.twitch.tv/oauth2/authorize",
                "https://id.twitch.tv/oauth2/token",
                "https://id.twitch.tv/oauth2/revoke",
                "https://" + Utils.GetCallbackDomainAndPort() + "/callback/twitch"
            )
        {
        }

        ~TwitchToken()
        {
        }
    };
}
