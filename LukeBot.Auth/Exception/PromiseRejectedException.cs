using LukeBot.Common;


namespace LukeBot.Auth
{
    public class PromiseRejectedException: Exception
    {
        public PromiseRejectedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
