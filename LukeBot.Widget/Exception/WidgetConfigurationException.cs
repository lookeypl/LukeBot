using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetConfigurationException: Exception
    {
        public WidgetConfigurationException(string fmt, params object[] args)
            : base(string.Format(fmt, args)) {}
    }
}
