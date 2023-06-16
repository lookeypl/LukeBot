using LukeBot.Config;


namespace LukeBot.API
{
    public class TwitchToken: Token
    {
        public TwitchToken(AuthFlow flow, string lbUser, string userId)
            : base(
                Constants.TWITCH_SERVICE_NAME,
                lbUser,
                userId,
                flow,
                "https://id.twitch.tv/oauth2/authorize",
                "https://id.twitch.tv/oauth2/token",
                "https://id.twitch.tv/oauth2/revoke",
                "https://" + Conf.Get<string>(Common.Constants.PROP_STORE_SERVER_IP_PROP) + "/callback/twitch"
            )
        {
        }

        ~TwitchToken()
        {
        }
    };
}
