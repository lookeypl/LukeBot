using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LukeBot.Tests.Twitch.Impl")]

namespace LukeBot.Twitch.Impl
{
    internal class Constants
    {
        public static readonly string PROP_TWITCH_COMMANDS = "commands";
        public static readonly int RECONNECT_ATTEMPTS = 10;
        public static readonly string DEFAULT_CHAT_USER_COLOR = "#888888";
    }
}
