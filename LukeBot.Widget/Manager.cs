using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Common;
using Intercom = LukeBot.Core.Events.Intercom;


namespace LukeBot.Widget
{
    public class Manager: IModule
    {
        private Dictionary<string, IWidget> mWidgets = new Dictionary<string, IWidget>();

        private Mutex mMutex;

        Intercom::ResponseBase ResponseAllocator(Intercom::MessageBase msg)
        {
            switch (msg.Message)
            {
            case Intercom::Messages.GET_WIDGET_PAGE: return new Intercom::GetWidgetPageResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new Intercom::ResponseBase();
        }

        public Manager()
        {
            mMutex = new Mutex();

            Intercom::EndpointInfo widgetManagerInfo = new Intercom::EndpointInfo(Intercom::Endpoints.WIDGET_MANAGER, ResponseAllocator);
            widgetManagerInfo.AddMessage(Intercom::Messages.GET_WIDGET_PAGE, GetWidgetPageDelegate);

            Core.Systems.Intercom.Register(widgetManagerInfo);
        }

        ~Manager()
        {
        }

        public void Init()
        {
        }

        public void Run()
        {
        }

        void GetWidgetPageDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            Intercom::GetWidgetPageMessage m = (Intercom::GetWidgetPageMessage)msg;
            Intercom::GetWidgetPageResponse r = (Intercom::GetWidgetPageResponse)resp;

            mMutex.WaitOne();

            if (!mWidgets.ContainsKey(m.widgetID))
            {
                mMutex.ReleaseMutex();
                r.SignalError(String.Format("Widget {0} not found", m.widgetID));
                return;
            }

            string ret = mWidgets[m.widgetID].GetPage();

            mMutex.ReleaseMutex();

            r.SetContents(ret);
            r.SignalSuccess();
        }

        public string Register(IWidget widget, string ID)
        {
            string id = ID;
            if (id.Length == 0)
            {
                Guid uuid = Guid.NewGuid();
                id = uuid.ToString();
            }

            mMutex.WaitOne();

            mWidgets.Add(id, widget);
            widget.SetID(id);

            mMutex.ReleaseMutex();

            return id;
        }

        public void Unregister(IWidget widget)
        {
            if (!mWidgets.Remove(widget.ID))
                Logger.Log().Warning("Cannot remove widget of ID {0} - not registered", widget.ID);
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
