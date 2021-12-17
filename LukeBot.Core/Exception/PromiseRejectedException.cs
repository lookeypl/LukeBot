using LukeBot.Common;


namespace LukeBot.Core
{
    public class PromiseRejectedException: Exception
    {
        public PromiseRejectedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
