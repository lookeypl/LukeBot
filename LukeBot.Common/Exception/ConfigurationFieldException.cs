namespace LukeBot.Common
{
    public class ConfigurationFieldException: Exception
    {
        public ConfigurationFieldException(string fmt, params object[] args)
            : base(string.Format(fmt, args))
        {
        }
    }
}