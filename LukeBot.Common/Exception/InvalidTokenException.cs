using System;

namespace LukeBot.Common.Exception
{
    public class InvalidTokenException: System.Exception
    {
        public InvalidTokenException(string msg): base(msg) {}
    }
}
