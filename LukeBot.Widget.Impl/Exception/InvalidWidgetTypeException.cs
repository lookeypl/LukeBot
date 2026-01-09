using LukeBot.Common;

namespace LukeBot.Widget.Impl
{
    public class InvalidWidgetTypeException: Exception
    {
        public InvalidWidgetTypeException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
