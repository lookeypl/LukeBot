using LukeBot.Common;

namespace LukeBot.Twitch.Impl
{
    public class UnknownChannelException: Exception
    {
        public UnknownChannelException(string channel)
            : base(string.Format("Unknown/not joined Twitch channel {1}", channel))
        {}
    }
}
