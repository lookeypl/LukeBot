using LukeBot.Common;


namespace LukeBot.Core
{
    public class NoFreePortException: Exception
    {
        public NoFreePortException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
