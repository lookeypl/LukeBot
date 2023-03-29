using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetNotFoundException: Exception
    {
        public WidgetNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
