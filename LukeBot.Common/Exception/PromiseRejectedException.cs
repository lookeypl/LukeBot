using System;

namespace LukeBot.Common
{
    public class PromiseRejectedException: System.Exception
    {
        public PromiseRejectedException(string msg): base(msg) {}
    }
}
