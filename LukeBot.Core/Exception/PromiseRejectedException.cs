using LukeBot.Common;


namespace LukeBot.Core
{
    public class PromiseRejectedException: Exception
    {
        public PromiseRejectedException(string msg): base(msg) {}
    }
}
