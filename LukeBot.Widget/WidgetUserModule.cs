using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using LukeBot.Config;
using LukeBot.Module;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class WidgetUserModule: IUserModule
    {
        private Dictionary<string, IWidget> mWidgets = new();
        private string mLBUser;


        private Path GetWidgetCollectionPropertyName()
        {
            return Path.Start()
                .Push(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(mLBUser)
                .Push(LukeBot.Common.Constants.WIDGET_MODULE_NAME)
                .Push(Constants.PROP_WIDGETS);
        }

        private void LoadWidgetsFromConfig()
        {
            Path widgetCollectionProp = GetWidgetCollectionPropertyName();

            WidgetDesc[] widgets;
            if (!Conf.TryGet<WidgetDesc[]>(widgetCollectionProp, out widgets))
                return; // quiet exit, assume user does not have any widgets

            foreach (WidgetDesc wd in widgets)
                LoadWidget(wd);
        }

        private void SaveWidgetToConfig(IWidget w)
        {
            WidgetDesc wd = w.GetDesc();

            Path widgetCollectionProp = GetWidgetCollectionPropertyName();
            ConfUtil.ArrayAppend(widgetCollectionProp, wd, new WidgetDesc.Comparer());
        }

        private void RemoveWidgetFromConfig(string id)
        {
            Path widgetCollectionProp = GetWidgetCollectionPropertyName();
            ConfUtil.ArrayRemove<WidgetDesc>(widgetCollectionProp, (WidgetDesc d) => d.Id != id);
        }

        private IWidget AllocateWidget(WidgetType type, string id, string name)
        {
            switch (type)
            {
            case WidgetType.echo: return new Echo(id, name);
            case WidgetType.nowplaying: return new NowPlaying(mLBUser, id, name);
            case WidgetType.chat: return new Chat(mLBUser, id, name);
            case WidgetType.alerts: return new Alerts(mLBUser, id, name);
            default:
                throw new InvalidWidgetTypeException("Invalid widget type: {0}", type);
            }
        }


        internal IWidget LoadWidget(WidgetDesc wd)
        {
            IWidget w = AllocateWidget(wd.Type, wd.Id, wd.Name);
            mWidgets.Add(wd.Id, w);
            return w;
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


        // Public methods //

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

            SaveWidgetToConfig(w);

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

            RemoveWidgetFromConfig(id);
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

        public ModuleType GetModuleType()
        {
            return ModuleType.Widget;
        }
    }
}
