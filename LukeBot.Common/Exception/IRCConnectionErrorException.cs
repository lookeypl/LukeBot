namespace LukeBot.Common
{
    public class IRCConnectionErrorException: Exception
    {
        public IRCConnectionErrorException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
