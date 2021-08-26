using System;

namespace LukeBot.Auth
{
    public class LoginFailedException: System.Exception
    {
        public LoginFailedException(string msg): base(msg) {}
    }
}
