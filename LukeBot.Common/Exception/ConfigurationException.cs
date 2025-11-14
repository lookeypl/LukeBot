namespace LukeBot.Common
{
    public class ConfigurationException: Exception
    {
        public ConfigurationException(string fmt, params object[] args)
            : base(string.Format(fmt, args))
        {}

        public ConfigurationException(string msg, System.Exception inner)
            : base(msg, inner)
        {}
    }
}
