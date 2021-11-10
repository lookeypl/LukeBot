using LukeBot.Common;

namespace LukeBot.Auth
{
    public class SpotifyToken: Token
    {
        public SpotifyToken(AuthFlow flow, string tokenId)
            : base(
                Constants.SPOTIFY_SERVICE_NAME,
                tokenId,
                flow,
                "https://accounts.spotify.com/authorize",
                "https://accounts.spotify.com/api/token",
                "https://accounts.spotify.com/api/revoke",
                "https://" + Utils.GetConfigServerIP() + "/callback/spotify"
            )
        {
        }

        ~SpotifyToken()
        {
        }
    };
}
