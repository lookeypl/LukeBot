using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using LukeBot.Services;
using LukeBot.User.Common;

namespace LukeBot.Widget.Common
{
    public interface IWidgetUserModule: IUserModule
    {
        public string AddWidget(WidgetType type, string name);
        public Task AssignWidgetWebSocket(string id, WebSocket ws);
        public IEnumerable<WidgetDesc> ListWidgets();
        public WidgetDesc GetWidgetInfo(string id);
        public string GetWidgetPage(string id);
        public bool IsWidgetLoaded(string id);
        public void DeleteWidget(string id);
        public void ReloadWidget(string id);
        public void UpdateWidgetConfiguration(string id, IEnumerable<(string, string)> changes);
        public IWidgetConfiguration GetWidgetConfiguration(string id);

    }
}