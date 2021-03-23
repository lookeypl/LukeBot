using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public string Register(IWidget widget)
        {
            // TODO replace with random UUID
            string ID = "TEST-WIDGET-ID";

            mMutex.WaitOne();
            mWidgets.Add(ID, widget);
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
    }
}
