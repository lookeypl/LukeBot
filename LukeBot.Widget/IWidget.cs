using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LukeBot.Common;


namespace LukeBot.Widget
{
    public abstract class IWidget
    {
        public string ID { get; set; }
        private List<string> mHead;

        protected abstract string GetWidgetCode();
        public abstract void RequestShutdown();
        public abstract void WaitForShutdown();

        protected void AddToHead(string line)
        {
            Logger.Log().Secure("Adding head line {0}", line);
            mHead.Add(line);
        }

        public IWidget()
        {
            mHead = new List<string>();
        }

        public string GetPage()
        {
            string page = "<!DOCTYPE html><html><head>";

            // form head contents
            foreach (string h in mHead)
            {
                page += h;
            }

            page += "</head><body>";
            page += GetWidgetCode();
            page += "</body></html>";

            return page;
        }
    }
}
