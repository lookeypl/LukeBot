using LukeBot.Common;

namespace LukeBot.API
{
    public class InvalidClientDataException: Exception
    {
        public InvalidClientDataException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
