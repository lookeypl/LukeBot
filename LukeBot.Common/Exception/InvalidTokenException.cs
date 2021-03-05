using System;

namespace LukeBot.Common
{
    public class InvalidTokenException: System.Exception
    {
        public InvalidTokenException(string msg): base(msg) {}
    }
}
