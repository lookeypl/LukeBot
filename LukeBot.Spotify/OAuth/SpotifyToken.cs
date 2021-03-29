using LukeBot.Common.OAuth;

namespace LukeBot.Spotify.OAuth
{
    public class SpotifyToken: Token
    {
        public SpotifyToken(AuthFlow flow)
            : base(
                Constants.SERVICE_NAME,
                flow,
                "https://accounts.spotify.com/authorize",
                "https://accounts.spotify.com/api/token",
                "https://accounts.spotify.com/api/revoke",
                "http://localhost:5000/callback/spotify"
            )
        {
        }

        ~SpotifyToken()
        {
        }
    };
}
