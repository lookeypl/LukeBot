namespace LukeBot.Common
{
    public class UnsupportedPlatformException: Exception
    {
        public UnsupportedPlatformException(string msg): base(msg) {}
    }
}
