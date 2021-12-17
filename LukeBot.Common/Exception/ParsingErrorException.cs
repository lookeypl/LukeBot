namespace LukeBot.Common
{
    public class ParsingErrorException: Exception
    {
        public ParsingErrorException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
