using LukeBot.Common;

namespace LukeBot
{
    public class InvalidSpotifyURLException: Exception
    {
        public InvalidSpotifyURLException(string url)
            : base(string.Format("Invalid Spotify URL: {0}", url))
        {
        }
    }
}