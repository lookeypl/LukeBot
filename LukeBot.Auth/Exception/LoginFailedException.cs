using LukeBot.Common;


namespace LukeBot.Auth
{
    public class LoginFailedException: Exception
    {
        public LoginFailedException(string msg): base(msg) {}
    }
}
