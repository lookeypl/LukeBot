using System;
using System.Collections.Generic;
using LukeBot.Common;


namespace LukeBot.Widget.Common
{
    public class AudioPlayTrigger
    {
        public string RedemptionName;
        public List<string> Files;
        public bool RepeatTotalTime;

        public string Get(string field)
        {
            return "";
        }

        public void Set(string field, string value)
        {

        }

        public override string ToString()
        {
            return String.Format("<{0}, [{1}], {2}>", RedemptionName, String.Join(", ", Files), RepeatTotalTime);
        }
    }

    public class AudioPlayConfig: Configuration<AudioPlayConfig>
    {
        public List<AudioPlayTrigger> Triggers { get; set; }

        public AudioPlayConfig()
        {
        }
    }
}