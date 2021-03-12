using System;

namespace LukeBot.Twitch
{
    public class ParsingErrorException: System.Exception
    {
        public ParsingErrorException(string msg): base(msg) {}
    }
}
