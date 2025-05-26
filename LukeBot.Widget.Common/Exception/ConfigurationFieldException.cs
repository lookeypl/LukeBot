using LukeBot.Common;

namespace LukeBot.Widget.Common
{
    public class ConfigurationFieldException: LukeBot.Common.Exception
    {
        public ConfigurationFieldException(string fmt, params object[] args)
            : base(string.Format(fmt, args))
        {
        }
    }
}