using System;

namespace LukeBot.Common.Exception
{
    public class PromiseRejectedException: System.Exception
    {
        public PromiseRejectedException(string msg): base(msg) {}
    }
}
