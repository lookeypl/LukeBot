namespace LukeBot.Core
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
            Systems.Connection.ReleasePort(Value);
        }
    }
}