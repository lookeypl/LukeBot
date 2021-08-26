namespace LukeBot.Auth
{
    public class TwitchToken: Token
    {
        public TwitchToken(AuthFlow flow)
            : base(
                Constants.TWITCH_SERVICE_NAME,
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
