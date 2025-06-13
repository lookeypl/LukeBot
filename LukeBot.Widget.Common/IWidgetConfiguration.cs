using System.Collections.Generic;
using LukeBot.Common;


namespace LukeBot.Widget.Common
{
    public interface IWidgetConfiguration
    {
        public Dictionary<string, ConfigurationField> GetFields();
        public ConfigurationField Get(string name);
        public ConfigurationFieldAccessor<T> Get<T>(string name);
    }
}
