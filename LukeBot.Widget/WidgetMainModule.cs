using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Widget.Common;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Widget
{
    public class WidgetMainModule
    {
        private Dictionary<string, WidgetUserModule> mUsers;
        private Dictionary<string, string> mWidgetIDToUser;
        private Mutex mMutex;


        Intercom::ResponseBase ResponseAllocator(Intercom::MessageBase msg)
        {
            switch (msg.Message)
            {
            case Intercom::Messages.GET_WIDGET_PAGE: return new Intercom::GetWidgetPageResponse();
            case Intercom::Messages.ASSIGN_WS: return new Intercom::AssignWSResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new Intercom::ResponseBase();
        }

        void GetWidgetPageDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            Intercom::GetWidgetPageMessage m = (Intercom::GetWidgetPageMessage)msg;
            Intercom::GetWidgetPageResponse r = (Intercom::GetWidgetPageResponse)resp;

            mMutex.WaitOne();

            string ret;

            try
            {
                string user = mWidgetIDToUser[m.widgetID];
                ret = mUsers[user].GetWidgetPage(m.widgetID);
            }
            catch (System.Exception e)
            {
                mMutex.ReleaseMutex();
                r.SignalError(String.Format("Widget {0} couldn't be acquired: {1}", m.widgetID, e.Message));
                return;
            }

            mMutex.ReleaseMutex();

            r.SetContents(ret);
            r.SignalSuccess();
        }

        void AssignWSDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            Intercom::AssignWSMessage m = (Intercom::AssignWSMessage)msg;
            Intercom::AssignWSResponse r = (Intercom::AssignWSResponse)resp;

            mMutex.WaitOne();

            try
            {
                string user = mWidgetIDToUser[m.widgetID];
                r.SetLifetimeTask(mUsers[user].AssignWS(m.widgetID, m.GetWebSocket()));
            }
            catch (System.Exception e)
            {
                mMutex.ReleaseMutex();
                r.SignalError(String.Format("Widget {0} WS assignment failed: {1}", m.widgetID, e.Message));
                return;
            }

            mMutex.ReleaseMutex();
            resp.SignalSuccess();
        }

        public WidgetMainModule()
        {
            mUsers = new Dictionary<string, WidgetUserModule>();
            mWidgetIDToUser = new Dictionary<string, string>();
            mMutex = new Mutex();

            Intercom::EndpointInfo widgetManagerInfo = new Intercom::EndpointInfo(Intercom::Endpoints.WIDGET_MANAGER, ResponseAllocator);
            widgetManagerInfo.AddMessage(Intercom::Messages.GET_WIDGET_PAGE, GetWidgetPageDelegate);
            widgetManagerInfo.AddMessage(Intercom::Messages.ASSIGN_WS, AssignWSDelegate);

            Comms.Intercom.Register(widgetManagerInfo);
        }

        public WidgetUserModule LoadWidgetUserModule(string lbUser)
        {
            if (mUsers.ContainsKey(lbUser))
            {
                throw new WidgetUserAlreadyLoadedException("Widget user {0} already loaded", lbUser);
            }

            WidgetUserModule user = new WidgetUserModule(lbUser);

            mUsers.Add(lbUser, user);

            Logger.Log().Secure("Loaded Widget user {0}", lbUser);
            return user;
        }

        public string AddWidget(string lbUser, WidgetType type, string name)
        {
            IWidget w = mUsers[lbUser].AddWidget(type, name);
            mWidgetIDToUser.Add(w.ID, lbUser);
            return w.GetWidgetAddress();
        }

        public List<WidgetDesc> ListUserWidgets(string lbUser)
        {
            return mUsers[lbUser].ListWidgets();
        }

        public WidgetDesc GetWidgetInfo(string lbUser, string id)
        {
            return mUsers[lbUser].GetWidgetInfo(id);
        }

        public void DeleteWidget(string lbUser, string id)
        {
            mUsers[lbUser].DeleteWidget(id);
        }

        public void Run()
        {
        }

        public void RequestShutdown()
        {
            foreach (WidgetUserModule um in mUsers.Values)
                um.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (WidgetUserModule um in mUsers.Values)
                um.WaitForShutdown();
        }
    }
}