using System;

namespace LukeBot.Common
{
    public class LoginFailedException: System.Exception
    {
        public LoginFailedException(string msg): base(msg) {}
    }
}
