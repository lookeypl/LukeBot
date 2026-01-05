using System.Net;
using LukeBot.Common;

namespace LukeBot.Twitch.Impl
{
    public class EventSubConnectFailedException: Exception
    {
        public EventSubConnectFailedException()
            : base(string.Format("Failed to connect to EventSub"))
        {}
    }
}
