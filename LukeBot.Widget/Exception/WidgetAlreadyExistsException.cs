using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetAlreadyExistsException: Exception
    {
        public WidgetAlreadyExistsException(string name, string id)
            : base(string.Format("Widget {0} already exists under ID {1}", name, id))
        {}
    }
}
