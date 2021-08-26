using System;

namespace LukeBot.Auth
{
    public class InvalidTokenException: System.Exception
    {
        public InvalidTokenException(string msg): base(msg) {}
    }
}
