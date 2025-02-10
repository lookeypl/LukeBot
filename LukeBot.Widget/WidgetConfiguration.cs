using System;
using System.Collections.Generic;
using LukeBot.Communication.Common;
using Newtonsoft.Json;


namespace LukeBot.Widget.Common
{
    public abstract class WidgetConfiguration: EventArgsBase, IWidgetConfiguration
    {
        private Dictionary<string, WidgetConfigurationField> mFields;

        protected void AddField(string name, WidgetConfigurationFieldType type)
        {
            if (mFields.ContainsKey(name))
            {
                throw new WidgetConfigurationException()
            }
        }

        public WidgetConfiguration(string name)
            : base(name)
        {
        }

        public string SerializeConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public abstract void DeserializeConfiguration(string configString);
        public abstract void ValidateUpdate(string field, string value);
        public abstract void Update(string field, string value);
        public abstract string ToFormattedString();

        public Dictionary<string, WidgetConfigurationField> GetFields()
        {
            return mFields;
        }
    }
}
