using LukeBot.Config;


namespace LukeBot.API
{
    public class SpotifyToken: Token
    {
        public SpotifyToken(AuthFlow flow, string userId)
            : base(
                Constants.SPOTIFY_SERVICE_NAME,
                userId,
                flow,
                "https://accounts.spotify.com/authorize",
                "https://accounts.spotify.com/api/token",
                "https://accounts.spotify.com/api/revoke",
                "https://" + Conf.Get<string>(Common.Constants.PROP_STORE_SERVER_IP_PROP) + "/callback/spotify"
            )
        {
        }

        ~SpotifyToken()
        {
        }
    };
}
