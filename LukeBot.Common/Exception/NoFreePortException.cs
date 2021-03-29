using System;

namespace LukeBot.Common
{
    public class NoFreePortException: System.Exception
    {
        public NoFreePortException(string msg): base(msg) {}
    }
}
