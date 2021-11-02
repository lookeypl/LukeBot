using System;
using System.Collections.Generic;
using System.Threading;

namespace LukeBot.Common
{
    public class WidgetManager
    {
        private static readonly Lazy<WidgetManager> mInstance =
            new Lazy<WidgetManager>(() => new WidgetManager());
        public static WidgetManager Instance { get { return mInstance.Value; } }

        private Dictionary<string, IWidget> mWidgets = new Dictionary<string, IWidget>();

        private Mutex mMutex;

        private WidgetManager()
        {
            mMutex = new Mutex();
        }

        ~WidgetManager()
        {
        }

        public string Register(IWidget widget, string ID)
        {
            // TODO replace ID with random UUID
            mMutex.WaitOne();
            mWidgets.Add(ID, widget);
            widget.ID = ID;
            mMutex.ReleaseMutex();

            return ID;
        }

        public string GetWidgetPage(string widgetID)
        {
            mMutex.WaitOne();

            if (!mWidgets.ContainsKey(widgetID))
            {
                mMutex.ReleaseMutex();
                return "WIDGET NOT FOUND";
            }

            string ret = mWidgets[widgetID].GetPage();

            mMutex.ReleaseMutex();

            return ret;
        }

        public void Unregister(IWidget widget)
        {
            if (!mWidgets.Remove(widget.ID))
                Logger.Log().Warning("Cannot remove widget of ID {0} - not registered", widget.ID);
        }
    }
}
