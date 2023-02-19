using LukeBot.Common;
using Newtonsoft.Json;


namespace LukeBot.Config
{
    class PropertyStorePrintVisitor : PropertyStoreVisitor
    {
        private int mTabCount = 0;
        private LogLevel mLogLevel;
        private bool mShowHidden;


        private string GetCurrentTabs()
        {
            string tabs = "";
            for (int i = 0; i < mTabCount; ++i)
            {
                tabs += "    ";
            }

            return tabs;
        }


        public PropertyStorePrintVisitor(LogLevel level)
        {
            mLogLevel = level;
            mShowHidden = false;
        }

        public PropertyStorePrintVisitor(LogLevel level, bool showHidden)
        {
            mLogLevel = level;
            mShowHidden = showHidden;

            if (mShowHidden)
                Logger.Log().Warning("!! Will print hidden properties !!");
        }

        public void Visit<T>(PropertyType<T> p)
        {
            if (mShowHidden || (!mShowHidden && !p.Hidden))
                Logger.Log().Message(mLogLevel, "{0}{1}{2} {3} = {4}",
                    GetCurrentTabs(),
                    p.Hidden ? "<hidden> " : "",
                    p.Type.ToString(),
                    p.Name,
                    JsonConvert.SerializeObject(p.Value));
        }

        public void VisitStart(PropertyDomain pd)
        {
            Logger.Log().Message(mLogLevel, "{0}\"{1}\" =", GetCurrentTabs(), pd.mName);
            Logger.Log().Message(mLogLevel, "{0}{{", GetCurrentTabs());
            mTabCount++;
        }

        public void VisitEnd(PropertyDomain pd)
        {
            mTabCount--;
            Logger.Log().Message(mLogLevel, "{0}}}", GetCurrentTabs());
        }
    }
}