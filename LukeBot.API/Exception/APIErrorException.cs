using LukeBot.Common;

namespace LukeBot.API
{
    public class APIErrorException: Exception
    {
        public APIErrorException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
