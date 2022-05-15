using LukeBot.Common;
using Newtonsoft.Json;


namespace LukeBot.Core
{
    class PropertyStorePrintVisitor : PropertyStoreVisitor
    {
        private int mTabCount = 0;
        private LogLevel mLogLevel;


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
        }

        public void Visit<T>(PropertyType<T> p)
        {
            Logger.Log().Message(mLogLevel, "     {0}\"{1}\" = {2}", GetCurrentTabs(), p.Name, JsonConvert.SerializeObject(p.Value));
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
            Logger.Log().Message(mLogLevel, "  {0}}}", GetCurrentTabs());
        }
    }
}