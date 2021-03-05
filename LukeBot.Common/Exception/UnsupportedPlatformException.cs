using System;

namespace LukeBot.Common
{
    public class UnsupportedPlatformException: System.Exception
    {
        public UnsupportedPlatformException(string msg): base(msg) {}
    }
}
