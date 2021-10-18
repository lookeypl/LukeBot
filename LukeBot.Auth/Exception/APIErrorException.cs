using System;

namespace LukeBot.Auth
{
    public class APIErrorException: System.Exception
    {
        public APIErrorException(string msg): base(msg) {}
    }
}
