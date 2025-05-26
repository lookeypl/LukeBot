using System;
using System.Collections.Generic;
using LukeBot.Communication.Common;
using LukeBot.Widget.Common;
using LukeBot.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace LukeBot.Widget
{
    internal class ConfigurationJsonConverter: JsonConverter<Configuration>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Configuration).IsAssignableFrom(typeToConvert);
        }

        private void ReadElement(JsonElement element, Configuration conf)
        {
            JsonElement.ObjectEnumerator enumerator = element.EnumerateObject();
            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            foreach (JsonProperty prop in enumerator)
            {
                string name = prop.Name;
                if (!fields.ContainsKey(name)) continue;

                switch (prop.Value.ValueKind)
                {
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    break;
                default:
                    fields[name].SetJson(prop.Value);
                    break;
                }
            }
        }

        public override Configuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                string eventName = doc.RootElement.GetProperty("EventName").GetString();

                Configuration conf = Configuration.AllocateInstanceOf(eventName);
                ReadElement(doc.RootElement, conf);
                return conf;
            }
        }

        public sealed override void Write(Utf8JsonWriter writer, Configuration configuration, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            Dictionary<string, ConfigurationField> fields = configuration.GetFields();
            foreach (ConfigurationField field in fields.Values)
            {
                writer.WritePropertyName(field.Name);
                field.GetJson().WriteTo(writer);
            }

            // remember to also add EventName field
            writer.WriteString(nameof(configuration.EventName), configuration.EventName);

            writer.WriteEndObject();
        }
    }
}

