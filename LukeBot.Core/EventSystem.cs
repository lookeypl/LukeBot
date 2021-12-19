using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using LukeBot.Common;


namespace LukeBot.Core
{
    public interface IEventPublisher
    {
    }

    [Flags]
    public enum EventType
    {
        None = 0,
        TwitchChatMessage = 0x1,
        TwitchChatMessageClear = 0x2,
        TwitchChatUserClear = 0x4,
        MusicStateUpdate = 0x8,
        MusicTrackChanged = 0x10,
        TwitchChannelPointsRedemption = 0x20,
        TwitchSubscription = 0x40,
        TwitchBitsCheer = 0x80,
    }

    public class EventArgsBase
    {
        public EventType eventType { get; private set; }

        public EventArgsBase(EventType type)
        {
            eventType = type;
        }
    }

    public class TwitchChatMessageArgs: EventArgsBase
    {
        public string user { get; private set; }
        public string message { get; private set; }

        public TwitchChatMessageArgs(string u, string m)
            : base(EventType.TwitchChatMessage)
        {
            user = u;
            message = m;
        }
    }

    public class TwitchChatMessageClearArgs: EventArgsBase
    {
        public TwitchChatMessageClearArgs()
            : base(EventType.TwitchChatMessageClear)
        {
        }
    }

    public class TwitchChannelPointsRedemptionArgs: EventArgsBase
    {
        public string name { get; set; }
        public string user { get; set; }
        public int points { get; set; }

        public TwitchChannelPointsRedemptionArgs()
            : base(EventType.TwitchChannelPointsRedemption)
        {
        }
    }

    public delegate void PublishEventDelegate(EventArgsBase args);

    public struct EventCallback
    {
        public EventType type { get; private set; }
        public PublishEventDelegate PublishEvent { get; private set; }

        public EventCallback(EventType t, PublishEventDelegate pe)
        {
            type = t;
            PublishEvent = pe;
        }
    }

    public class EventSystem
    {
        public event EventHandler<EventArgsBase> TwitchChatMessage;
        public event EventHandler<EventArgsBase> TwitchChatMessageClear;
        public event EventHandler<EventArgsBase> TwitchChannelPointsRedemption;

        private List<IEventPublisher> mPublishers;
        private Dictionary<string, Func<EventSystem, EventHandler<EventArgsBase>>> mEvents; // maps EventHandler's generic type (args struct) name to EventInfo


        //private

        private EventHandler<EventArgsBase> GetHandler(string name)
        {
            return mEvents[name](this);
        }

        public void OnEvent<T>(EventArgsBase args) where T: EventArgsBase
        {
            Debug.Assert(args is T, "Invalid type of arguments provided to callback - expected {0}");

            // emit event
            EventHandler<EventArgsBase> handler = GetHandler(typeof(T).Name);
            if (handler != null)
            {
                // TODO get event provider here and use it instead of `this`
                handler(this, args);
            }
        }

        private EventCallback CreateEventCallback(EventType type)
        {
            Type argsBaseType = typeof(EventArgsBase);
            Type argsType = Type.GetType("LukeBot.Core." + type.ToString() + "Args");
            if (argsType == null)
            {
                throw new EventTypeNotFoundException("Could not acquire arguments for event type: {0}", type);
            }

            MethodInfo eventMethod = typeof(EventSystem).GetMethod(nameof(EventSystem.OnEvent), 1, new Type[] { argsBaseType }).MakeGenericMethod(argsType);
            return new EventCallback(type, (PublishEventDelegate)Delegate.CreateDelegate(typeof(PublishEventDelegate), this, eventMethod));
        }


        public EventSystem()
        {
            mPublishers = new List<IEventPublisher>();

            mEvents = new Dictionary<string, Func<EventSystem, EventHandler<EventArgsBase>>>();
            mEvents.Add("TwitchChatMessageArgs", x => x.TwitchChatMessage);
            mEvents.Add("TwitchChatMessageClearArgs", x => x.TwitchChatMessageClear);
            mEvents.Add("TwitchChannelPointsRedemptionArgs", x => x.TwitchChannelPointsRedemption);
        }

        ~EventSystem()
        {
        }

        // Register an Event Publisher.
        // Returns a list of callbacks which should be called to emit an event + for what event type it is for.
        // Returned List will have as many callbacks as there were EventType's provided in @p type
        public List<EventCallback> RegisterEventPublisher(IEventPublisher publisher, EventType type)
        {
            mPublishers.Add(publisher);

            List<EventCallback> callbackList = new List<EventCallback>();

            foreach (EventType t in Enum.GetValues(typeof(EventType)))
            {
                if ((t & type) != EventType.None)
                {
                    callbackList.Add(CreateEventCallback(t));
                }
            }

            return callbackList;
        }
    }
}
