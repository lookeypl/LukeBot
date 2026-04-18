using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace LukeBot.Common
{
    internal class ConfigurationJsonConverter<Configurable>: JsonConverter<Configurable>
        where Configurable: Configuration<Configurable>, new()
    {
        private bool IncludeHidden { get; init; }

        public ConfigurationJsonConverter()
            : this(false)
        {
        }

        public ConfigurationJsonConverter(bool includeHidden)
        {
            IncludeHidden = includeHidden;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ConfigurationBase).IsAssignableFrom(typeToConvert);
        }

        private void ReadElement(JsonElement element, Configurable conf)
        {
            JsonElement.ObjectEnumerator enumerator = element.EnumerateObject();
            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            foreach (JsonProperty prop in enumerator)
            {
                string name = prop.Name;
                if (!fields.ContainsKey(name)) continue;
                if (!fields[name].IsRoot) continue;
                if (!fields[name].Serializable) continue;

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

        public override Configurable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                string eventName = doc.RootElement.GetProperty("FullConfigurableTypeName").GetString();

                Configurable conf = ConfigurationFactory.AllocateInstanceOf(eventName) as Configurable;
                ReadElement(doc.RootElement, conf);
                return conf;
            }
        }

        public sealed override void Write(Utf8JsonWriter writer, Configurable configuration, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            Dictionary<string, ConfigurationField> fields = configuration.GetFields();
            foreach (ConfigurationField field in fields.Values)
            {
                if (!IncludeHidden && !field.Visible) continue;
                if (!field.Serializable) continue;

                // TODO while this prevents writing sub-objects as "separate" objects,
                // this also will simply write down every single field inside that object, not just ConfigurationFieldAttribute-ones
                // This needs adjusting, most likely some deeper inspection based on the Dictionary above
                if (field.IsRoot)
                {
                    writer.WritePropertyName(field.Name);
                    field.GetJson().WriteTo(writer);
                }
            }

            // remember to also add other important fields
            writer.WriteString(nameof(configuration.EventName), configuration.EventName);
            writer.WriteString(nameof(configuration.EventID), configuration.EventID);
            writer.WriteString(nameof(configuration.FullConfigurableTypeName), configuration.FullConfigurableTypeName);

            writer.WriteEndObject();
        }
    }
}

