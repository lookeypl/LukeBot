using System;

namespace LukeBot.Common
{
    public class ParsingErrorException: System.Exception
    {
        public ParsingErrorException(string msg): base(msg) {}
    }
}
