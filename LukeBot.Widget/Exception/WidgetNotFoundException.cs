using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetNotFoundException: Exception
    {
        public WidgetNotFoundException(string id)
            : base(string.Format("Widget {0} not found", id))
        {}
    }
}
