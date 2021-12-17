namespace LukeBot.Common
{
    public class UnsupportedPlatformException: Exception
    {
        public UnsupportedPlatformException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
