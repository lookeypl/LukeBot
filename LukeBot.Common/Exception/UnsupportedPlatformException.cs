using System;

namespace LukeBot.Common.Exception
{
    public class UnsupportedPlatformException: System.Exception
    {
        public UnsupportedPlatformException(string msg): base(msg) {}
    }
}
