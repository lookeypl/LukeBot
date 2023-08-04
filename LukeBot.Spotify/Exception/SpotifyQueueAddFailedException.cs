using System.Net;
using LukeBot.Common;

namespace LukeBot
{
    public class SpotifyQueueAddFailedException: Exception
    {
        public SpotifyQueueAddFailedException(HttpStatusCode code)
            : base(string.Format("Failed to add song to queue: {0} (Luke will know what this means)", code))
        {
        }
    }
}