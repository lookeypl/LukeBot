using LukeBot.Common;

namespace LukeBot.Auth
{
    public class InvalidTokenException: Exception
    {
        public InvalidTokenException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
