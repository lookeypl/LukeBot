using System;

namespace LukeBot.Core
{
    public class PromiseRejectedException: System.Exception
    {
        public PromiseRejectedException(string msg): base(msg) {}
    }
}
