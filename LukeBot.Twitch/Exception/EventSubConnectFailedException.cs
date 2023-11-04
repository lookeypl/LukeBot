using System.Net;
using LukeBot.Common;

namespace LukeBot.Twitch
{
    public class EventSubConnectFailedException: Exception
    {
        public EventSubConnectFailedException()
            : base(string.Format("Failed to connect to EventSub"))
        {}
    }
}
