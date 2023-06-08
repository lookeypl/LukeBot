using LukeBot.Common;

namespace LukeBot.Twitch
{
    public class ChannelAlreadyJoinedException: Exception
    {
        public ChannelAlreadyJoinedException(string channel)
            : base(string.Format("Channel {0} already joined", channel))
        {}
    }
}
