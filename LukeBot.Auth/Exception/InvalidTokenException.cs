using LukeBot.Common;

namespace LukeBot.Auth
{
    public class InvalidTokenException: Exception
    {
        public InvalidTokenException(string msg): base(msg) {}
    }
}
