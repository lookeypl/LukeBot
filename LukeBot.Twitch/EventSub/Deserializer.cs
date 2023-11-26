using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LukeBot.Logging;


namespace LukeBot.Twitch.EventSub
{
    public abstract class DeserializerBase: JsonConverter
    {
        public override bool CanWrite { get { return false; } }

        public override bool CanConvert(Type objectType)
        {
            return (objectType.Namespace != null) && (objectType.Namespace.Equals("LukeBot.Twitch.EventSub"));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Json Writes are not supported by EventSub Deserializer");
        }
    }


    public class Deserializer: DeserializerBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            if (!obj.TryGetValue("metadata", out JToken metadataToken))
            {
                Logger.Log().Error("Failed to find metadata info");
                return null;
            }

            Message message = new();
            message.Metadata = metadataToken.ToObject<Metadata>();
            message.Payload = new();

            switch (message.Metadata.message_type)
            {
            case MessageType.session_keepalive:
                break;
            case MessageType.session_welcome:
            case MessageType.session_reconnect:
                message.Payload.Session = obj["payload"]["session"].ToObject<PayloadSession>();
                break;
            case MessageType.notification:
                message.Payload.Subscription = obj["payload"]["subscription"].ToObject<PayloadSubscription>();
                // depending on what data we have from Subscription.type we must allocate different object
                switch (message.Payload.Subscription.type)
                {
                case EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD:
                    message.Payload.Event = obj["payload"]["event"].ToObject<PayloadChannelPointRedemptionEvent>();
                    break;
                case EventSubClient.SUB_SUBSCRIBE:
                    message.Payload.Event = obj["payload"]["event"].ToObject<PayloadSubEvent>();
                    break;
                case EventSubClient.SUB_SUBSCRIPTION_GIFT:
                    message.Payload.Event = obj["payload"]["event"].ToObject<PayloadSubGiftEvent>();
                    break;
                case EventSubClient.SUB_SUBSCRIPTION_MESSAGE:
                    message.Payload.Event = obj["payload"]["event"].ToObject<PayloadSubMessageEvent>();
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