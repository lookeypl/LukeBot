using LukeBot.Common;

namespace LukeBot.Twitch
{
    public class ChannelAlreadyJoinedException: Exception
    {
        public ChannelAlreadyJoinedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
