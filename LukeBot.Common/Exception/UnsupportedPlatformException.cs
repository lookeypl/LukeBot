using System;

namespace LukeBot.Common.Exception
{
    class UnsupportedPlatformException: System.Exception
    {
        public UnsupportedPlatformException(string msg): base(msg) {}
    }
}
