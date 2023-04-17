using System.Collections.Generic;
using Newtonsoft.Json;

namespace LukeBot.Widget.Common
{
    public class WidgetDesc
    {
        public class Comparer: IComparer<WidgetDesc>
        {
            public int Compare(WidgetDesc a, WidgetDesc b)
            {
                return string.Compare(a.Id, b.Id);
            }
        }

        public WidgetType Type { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public string Address { get; set; }

        public string ToFormattedString()
        {
            string ret = "";

            ret += "  Id: " + Id;
            ret += "\n  Type: " + Type.ToString();
            ret += "\n  Name: " + Name;
            ret += "\n  Address: " + Address;

            return ret;
        }
    }
}