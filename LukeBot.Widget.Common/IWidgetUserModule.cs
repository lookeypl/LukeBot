using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using LukeBot.Services;
using LukeBot.User.Common;

namespace LukeBot.Widget.Common
{
    public interface IWidgetUserModule: IUserModule
    {
        // TODO this should be refactored further:
        //  - IWidget should be exposed in Widget.Common
        //  - This class should return an IWidget and eventually translate id to actual id
        //  - Most methods (get page, get info, get/save config, etc) should be done by IWidget
        //  - Loading and Saving Configuration should probably be done by Configuration itself
        public string AddWidget(WidgetType type, string name);
        public Task AssignWidgetWebSocket(string id, WebSocket ws);
        public IEnumerable<WidgetDesc> ListWidgets();
        public string GetActualWidgetId(string id);
        public WidgetDesc GetWidgetInfo(string id);
        public string GetWidgetPage(string id);
        public bool IsWidgetLoaded(string id);
        public void DeleteWidget(string id);
        public void ReloadWidget(string id);
        public IWidgetConfiguration GetWidgetConfiguration(string id);
        public void SaveConfiguration(string id);
    }
}