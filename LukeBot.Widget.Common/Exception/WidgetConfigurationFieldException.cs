using LukeBot.Common;

namespace LukeBot.Widget.Common
{
    public class WidgetConfigurationFieldException: LukeBot.Common.Exception
    {
        public WidgetConfigurationFieldException(string fmt, params object[] args)
            : base(string.Format(fmt, args))
        {
        }
    }
}