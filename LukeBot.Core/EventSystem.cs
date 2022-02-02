using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using LukeBot.Common;
using LukeBot.Core.Events;


namespace LukeBot.Core
{
    public interface IEventPublisher
    {
    }

    public delegate void PublishEventDelegate(EventArgsBase args);

    public struct EventCallback
    {
        public Events.Type type { get; private set; }
        public PublishEventDelegate PublishEvent { get; private set; }

        public EventCallback(Events.Type t, PublishEventDelegate pe)
        {
            type = t;
            PublishEvent = pe;
        }
    }

    public class EventSystem
    {
        public event EventHandler<EventArgsBase> TwitchChatMessage;
        public event EventHandler<EventArgsBase> TwitchChatMessageClear;
        public event EventHandler<EventArgsBase> TwitchChatUserClear;
        public event EventHandler<EventArgsBase> SpotifyMusicStateUpdate;
        public event EventHandler<EventArgsBase> SpotifyMusicTrackChanged;
        public event EventHandler<EventArgsBase> TwitchChannelPointsRedemption;
        public event EventHandler<EventArgsBase> TwitchSubscription;
        public event EventHandler<EventArgsBase> TwitchBitsCheer;

        private List<IEventPublisher> mPublishers;
        private Dictionary<string, Func<EventSystem, EventHandler<EventArgsBase>>> mEvents; // maps EventHandler's generic type (args struct) name to EventInfo


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

        private EventCallback CreateEventCallback(Events.Type type)
        {
            System.Type argsBaseType = typeof(EventArgsBase);
            System.Type argsType = EventUtils.GetEventTypeArgs(type.ToString());
            if (argsType == null)
            {
                throw new EventTypeNotFoundException("Could not acquire arguments for event type: {0}", type);
            }

            MethodInfo eventMethod = typeof(EventSystem).GetMethod(nameof(EventSystem.OnEvent), 1, new System.Type[] { argsBaseType }).MakeGenericMethod(argsType);
            return new EventCallback(type, (PublishEventDelegate)Delegate.CreateDelegate(typeof(PublishEventDelegate), this, eventMethod));
        }


        public EventSystem()
        {
            mPublishers = new List<IEventPublisher>();

            mEvents = new Dictionary<string, Func<EventSystem, EventHandler<EventArgsBase>>>();
            mEvents.Add("TwitchChatMessageArgs", x => x.TwitchChatMessage);
            mEvents.Add("TwitchChatMessageClearArgs", x => x.TwitchChatMessageClear);
            mEvents.Add("TwitchChatUserClearArgs", x => x.TwitchChatUserClear);
            mEvents.Add("SpotifyMusicStateUpdateArgs", x => x.SpotifyMusicStateUpdate);
            mEvents.Add("SpotifyMusicTrackChangedArgs", x => x.SpotifyMusicTrackChanged);
            mEvents.Add("TwitchChannelPointsRedemptionArgs", x => x.TwitchChannelPointsRedemption);
            mEvents.Add("TwitchSubscriptionArgs", x => x.TwitchSubscription);
            mEvents.Add("TwitchBitsCheerArgs", x => x.TwitchBitsCheer);
        }

        ~EventSystem()
        {
        }

        // Register an Event Publisher.
        // Returns a list of callbacks which should be called to emit an event + for what event type it is for.
        // Returned List will have as many callbacks as there were EventType's provided in @p type
        public List<EventCallback> RegisterEventPublisher(IEventPublisher publisher, Events.Type type)
        {
            mPublishers.Add(publisher);

            List<EventCallback> callbackList = new List<EventCallback>();

            foreach (Events.Type t in Enum.GetValues(typeof(Events.Type)))
            {
                if ((t & type) != Events.Type.None)
                {
                    callbackList.Add(CreateEventCallback(t));
                }
            }

            return callbackList;
        }
    }
}
