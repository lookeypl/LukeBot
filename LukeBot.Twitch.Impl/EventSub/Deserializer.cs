using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LukeBot.Logging;


namespace LukeBot.Twitch.Impl.EventSub
{
    internal class MessageDeserializer: JsonConverter<Message>
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType.Namespace != null) && (objectType.Namespace.Equals("LukeBot.Twitch.Impl.EventSub"));
        }

        public override void Write(Utf8JsonWriter writer, Message message, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Json Writes are not supported by EventSub Message Deserializer");
        }

        public override Message Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                JsonSerializerOptions innerOpts = new();
                innerOpts.Converters.Add(new JsonStringEnumConverter<MessageType>());

                Message message = new();
                message.Metadata = doc.RootElement.GetProperty("metadata").Deserialize<Metadata>(innerOpts);
                message.Payload = new();

                JsonElement payloadElement = doc.RootElement.GetProperty("payload");

                switch (message.Metadata.message_type)
                {
                case MessageType.session_keepalive:
                    break;
                case MessageType.session_welcome:
                case MessageType.session_reconnect:
                    message.Payload.Session = payloadElement.GetProperty("session").Deserialize<PayloadSession>();
                    break;
                case MessageType.notification:
                    message.Payload.Subscription = payloadElement.GetProperty("subscription").Deserialize<PayloadSubscription>();
                    // depending on what data we have from Subscription.type we must allocate different object
                    switch (message.Payload.Subscription.type)
                    {
                    case EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD:
                        message.Payload.Event = payloadElement.GetProperty("event").Deserialize<PayloadChannelPointRedemptionEvent>();
                        break;
                    case EventSubClient.SUB_SUBSCRIBE:
                        message.Payload.Event = payloadElement.GetProperty("event").Deserialize<PayloadSubEvent>();
                        break;
                    case EventSubClient.SUB_SUBSCRIPTION_GIFT:
                        message.Payload.Event = payloadElement.GetProperty("event").Deserialize<PayloadSubGiftEvent>();
                        break;
                    case EventSubClient.SUB_SUBSCRIPTION_MESSAGE:
                        message.Payload.Event = payloadElement.GetProperty("event").Deserialize<PayloadSubMessageEvent>();
                        break;
                    default:
                        Logger.Log().Error("Unsupported subscription type: {0}", message.Payload.Subscription.type);
                        return null;
                    }
                    break;
                default:
                    Logger.Log().Error("Unsupported message type: {0}", message.Metadata.message_type);
                    return null;
                }

                return message;
            }
        }
    }
}