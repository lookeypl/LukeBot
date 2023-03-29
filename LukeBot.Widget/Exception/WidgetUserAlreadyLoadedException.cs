using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetUserAlreadyLoadedException: Exception
    {
        public WidgetUserAlreadyLoadedException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
