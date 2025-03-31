using System.Collections.Generic;


namespace LukeBot.Widget.Common
{
    public interface IWidgetConfiguration
    {
        public Dictionary<string, WidgetConfigurationField> GetFields();
        public WidgetConfigurationField Get(string name);
        public WidgetConfigurationFieldAccessor<T> Get<T>(string name);
    }
}
