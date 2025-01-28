using System.Collections.Generic;
using LukeBot.Services;

namespace LukeBot.Widget.Common
{
    public interface IWidgetService: IService
    {
        // TODO replace lbUser with UserContext
        public string AddWidget(string lbUser, WidgetType type, string name);
        public List<WidgetDesc> ListUserWidgets(string lbUser);
        public WidgetDesc GetWidgetInfo(string lbUser, string id);
        public bool IsWidgetLoaded(string lbUser, string id);
        public void DeleteWidget(string lbUser, string id);
        public void ReloadWidget(string lbUser, string id);
        public void UpdateWidgetConfiguration(string lbUser, string id, IEnumerable<(string, string)> changes);
        public WidgetConfiguration GetWidgetConfiguration(string lbUser, string id);
    }
}