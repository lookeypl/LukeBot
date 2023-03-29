using LukeBot.Common;

namespace LukeBot.Widget
{
    public class InvalidWidgetTypeException: Exception
    {
        public InvalidWidgetTypeException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
