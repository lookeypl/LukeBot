using LukeBot.Common;


namespace LukeBot.Communication
{
    public class NoFreePortException: Exception
    {
        public NoFreePortException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
