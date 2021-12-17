using LukeBot.Common;

namespace LukeBot.Auth
{
    public class APIErrorException: Exception
    {
        public APIErrorException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
