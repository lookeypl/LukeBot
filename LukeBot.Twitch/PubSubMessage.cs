using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using LukeBot.Common;
using Newtonsoft.Json.Converters;


namespace LukeBot.Twitch
{
    public class PubSubMessage
    {
        public string type { get; set; }

        public PubSubMessage(string cmdType)
        {
            type = cmdType;
        }

        public virtual void Print(LogLevel level)
        {
            Logger.Log().Message(level, " -> type: {0}", type);
        }
    }

    public abstract class PubSubMessageData
    {
        public abstract void Print(LogLevel level);
    }

    public class PubSubCommand: PubSubMessage
    {
        public string nonce { get; private set; }
        public PubSubMessageData data { get; private set; }

        public PubSubCommand(string cmdType, PubSubMessageData cmdData)
            : base(cmdType)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] nonceData = new byte[32];
                rng.GetBytes(nonceData);
                nonce = Convert.ToBase64String(nonceData);
            }

            data = cmdData;
        }
    }

    public class PubSubListenCommandData: PubSubMessageData
    {
        public List<string> topics { get; private set; }
        public string auth_token { get; private set; }

        public PubSubListenCommandData(string topic, string authToken)
        {
            topics = new List<string>();
            topics.Add(topic);
            auth_token = authToken;
        }

        public override void Print(LogLevel level)
        {
            Logger.Log().Secure("   -> auth_token: {0}", auth_token);
            Logger.Log().Message(level, "   -> topics:", topics);
            foreach (string t in topics)
            {
                Logger.Log().Message(level, "     -> {0}", t);
            }
        }
    }

    public class PubSubResponse: PubSubMessage
    {
        public string error { get; set; }
        public string nonce { get; set; }

        public PubSubResponse(string type)
            : base(type)
        {
        }
    }

    public class PubSubReceivedMessageData: PubSubMessageData
    {
        public string topic { get; set; }
        public string message { get; set; }

        public override void Print(LogLevel logLevel)
        {
            Logger.Log().Message(logLevel, "   -> topic: {0}", topic);
            Logger.Log().Message(logLevel, "   -> message: {0}", message);
        }
    }

    public class PubSubTopicMessage: PubSubMessage
    {
        public PubSubMessageData data { get; set; }

        public PubSubTopicMessage(string type)
            : base(type)
        {
            data = null;
        }

        public override void Print(LogLevel level)
        {
            base.Print(level);
            Logger.Log().Message(level, " -> data:");
            data.Print(level);
        }
    }

    public class PubSubMessageDataCreationConverter: CustomCreationConverter<PubSubMessageData>
    {
        public override PubSubMessageData Create(Type objectType)
        {
            return new PubSubReceivedMessageData();
        }
    }
}
