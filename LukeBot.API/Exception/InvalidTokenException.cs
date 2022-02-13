using LukeBot.Common;

namespace LukeBot.API
{
    public class InvalidTokenException: Exception
    {
        public InvalidTokenException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
