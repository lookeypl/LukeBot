using LukeBot.Common;


namespace LukeBot.Auth
{
    public class LoginFailedException: Exception
    {
        public LoginFailedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
