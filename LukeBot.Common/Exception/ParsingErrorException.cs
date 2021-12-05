namespace LukeBot.Common
{
    public class ParsingErrorException: Exception
    {
        public ParsingErrorException(string msg): base(msg) {}
    }
}
