using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Widget.Common;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Widget
{
    public class WidgetUserModule: IModule
    {
        private Dictionary<string, IWidget> mWidgets = new Dictionary<string, IWidget>();
        private string mLBUser;


        private void LoadWidgetsFromConfig()
        {

        }

        private void AddWidgetToConfig(IWidget w)
        {

        }

        private void RemoveWidgetFromConfig(string id)
        {

        }

        private IWidget AllocateWidget(WidgetType type, string id, string name)
        {
            switch (type)
            {
            case WidgetType.echo: return new Echo(id, name);
            case WidgetType.nowplaying: return new NowPlaying(id, name);
            case WidgetType.chat: return new Chat(id, name);
            case WidgetType.alerts: return new Alerts(id, name);
            default:
                throw new InvalidWidgetTypeException("Invalid widget type: {0}", type);
            }
        }


        internal string GetWidgetPage(string widgetID)
        {
            if (!mWidgets.TryGetValue(widgetID, out IWidget widget))
                throw new WidgetNotFoundException("Widget {0} not found", widgetID);

            return widget.GetPage();
        }

        internal Task AssignWS(string widgetID, WebSocket ws)
        {
            if (!mWidgets.TryGetValue(widgetID, out IWidget widget))
            {
                throw new WidgetNotFoundException("Widget {0} not found", widgetID);
            }

            return widget.AcquireWS(ws);
        }

        public WidgetUserModule(string lbUser)
        {
            mLBUser = lbUser;

            LoadWidgetsFromConfig();
        }

        ~WidgetUserModule()
        {
        }

        public void Init()
        {
        }

        public void Run()
        {
        }

        public IWidget AddWidget(WidgetType type, string name)
        {
            string id = Guid.NewGuid().ToString();

            IWidget w = AllocateWidget(type, id, name);
            mWidgets.Add(id, w);

            return w;
        }

        public List<WidgetDesc> ListWidgets()
        {
            List<WidgetDesc> widgets = new List<WidgetDesc>();

            foreach (IWidget w in mWidgets.Values)
            {
                widgets.Add(w.GetDesc());
            }

            return widgets;
        }

        public WidgetDesc GetWidgetInfo(string id)
        {
            return mWidgets[id].GetDesc();
        }

        public void DeleteWidget(string id)
        {
            IWidget w = mWidgets[id];

            w.RequestShutdown();
            w.WaitForShutdown();

            mWidgets.Remove(id);
        }

        public void RequestShutdown()
        {
            foreach (var w in mWidgets)
            {
                w.Value.RequestShutdown();
            }
        }

        public void WaitForShutdown()
        {
            foreach (var w in mWidgets)
            {
                w.Value.WaitForShutdown();
            }
        }
    }
}
