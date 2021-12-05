using LukeBot.Common;

namespace LukeBot.Auth
{
    public class APIErrorException: Exception
    {
        public APIErrorException(string msg): base(msg) {}
    }
}
