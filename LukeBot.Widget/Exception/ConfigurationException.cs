using LukeBot.Common;

namespace LukeBot.Widget
{
    public class ConfigurationException: Exception
    {
        public ConfigurationException(string fmt, params object[] args)
            : base(string.Format(fmt, args)) {}
    }
}
