using LukeBot.Common;


namespace LukeBot.API
{
    public class SpotifyToken: Token
    {
        public SpotifyToken(AuthFlow flow, string lbUser)
            : base(
                Constants.SPOTIFY_SERVICE_NAME,
                lbUser,
                flow,
                "https://accounts.spotify.com/authorize",
                "https://accounts.spotify.com/api/token",
                "https://accounts.spotify.com/api/revoke",
                "https://" + Utils.GetCallbackDomainAndPort() + "/callback/spotify"
            )
        {
        }

        ~SpotifyToken()
        {
        }
    };
}
