using LukeBot.Common;


namespace LukeBot.API
{
    public class LoginFailedException: Exception
    {
        public LoginFailedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
