using System.Net;
using LukeBot.Common;

namespace LukeBot.Twitch
{
    public class EventSubSubscriptionFailedException: Exception
    {
        public EventSubSubscriptionFailedException(string sub, HttpStatusCode code)
            : base(string.Format("Failed to subscribe to EventSub event {0}: {1}", sub, code))
        {}
    }
}
