namespace LukeBot.Common
{
    public class ConnectionFailedException: Exception
    {
        public ConnectionFailedException(string msg)
            : base(string.Format("Connection attempt failed: {0}", msg))
        {}

        public ConnectionFailedException(string msg, System.Exception e)
            : base(string.Format("Connection attempt failed: {0}", msg), e)
        {}
    }
}
