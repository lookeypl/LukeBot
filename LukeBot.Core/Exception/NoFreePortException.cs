using System;

namespace LukeBot.Core
{
    public class NoFreePortException: System.Exception
    {
        public NoFreePortException(string msg): base(msg) {}
    }
}
