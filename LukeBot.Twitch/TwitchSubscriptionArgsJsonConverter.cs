using LukeBot.Twitch;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TwitchSubscriptionArgsJsonConverter: JsonConverter<TwitchSubscriptionArgs>
{
    public override TwitchSubscriptionArgs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("This conversion is not supported");
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(TwitchSubscriptionArgs).IsAssignableFrom(typeToConvert);
    }

    public override void Write(Utf8JsonWriter writer, TwitchSubscriptionArgs value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(nameof(value.EventName), value.EventName);
        writer.WriteString(nameof(value.User), value.User);
        writer.WriteString(nameof(value.DisplayName), value.DisplayName);

        writer.WritePropertyName(nameof(value.Details));

        switch (value.Details.Type)
        {
        case TwitchSubscriptionType.New:
            JsonSerializer.Serialize(writer, value.Details as TwitchSubscriptionDetails, options);
            break;
        case TwitchSubscriptionType.Resub:
            JsonSerializer.Serialize(writer, value.Details as TwitchResubscriptionDetails, options);
            break;
        case TwitchSubscriptionType.Gift:
            JsonSerializer.Serialize(writer, value.Details as TwitchGiftSubscriptionDetails, options);
            break;
        }

        writer.WriteEndObject();
    }
}
