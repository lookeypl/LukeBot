using LukeBot.Common;

namespace LukeBot.Widget.Impl
{
    public class WidgetSystemException: Exception
    {
        public WidgetSystemException(string msg, params object[] args)
            : base(string.Format(msg, args))
        {}
    }
}
