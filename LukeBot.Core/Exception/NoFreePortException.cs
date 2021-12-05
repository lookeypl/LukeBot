using LukeBot.Common;


namespace LukeBot.Core
{
    public class NoFreePortException: Exception
    {
        public NoFreePortException(string msg): base(msg) {}
    }
}
