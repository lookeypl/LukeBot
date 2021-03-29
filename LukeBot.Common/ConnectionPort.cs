namespace LukeBot.Common
{
    public class ConnectionPort
    {
        public int Value { get; private set; }

        public ConnectionPort(int port)
        {
            Value = port;
        }

        ~ConnectionPort()
        {
            ConnectionManager.Instance.ReleasePort(Value);
        }
    }
}