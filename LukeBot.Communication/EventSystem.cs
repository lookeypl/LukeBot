using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using LukeBot.Common;
using LukeBot.Communication.Events;


namespace LukeBot.Communication
{
    public interface IEventPublisher
    {
    }

    public abstract class EventCollection
    {
        private List<IEventPublisher> mPublishers = new();
        // maps EventHandler's generic type (args struct) name to EventInfo
        protected Dictionary<string, Func<EventCollection, EventHandler<EventArgsBase>>> mEvents = new();

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

        protected void AddPublisher(IEventPublisher p)
        {
            mPublishers.Add(p);
        }

        private MethodInfo GetEventMethodInfo(string typeStr)
        {
            System.Type argsBaseType = typeof(EventArgsBase);
            System.Type argsType = EventUtils.GetEventTypeArgs(typeStr);
            MethodInfo inf = typeof(EventCollection).GetMethod(nameof(EventCollection.OnEvent), 1, new System.Type[] { argsBaseType });
            if (inf == null)
            {
                throw new EventArgsNotFoundException(typeStr);
            }

            return inf.MakeGenericMethod(argsType);
        }

        protected EventCallback CreateEventCallback(UserEventType type)
        {
            MethodInfo eventMethod = GetEventMethodInfo(type.ToString());
            return new EventCallback(type, Delegate.CreateDelegate(typeof(PublishEventDelegate), this, eventMethod) as PublishEventDelegate);
        }

        protected EventCallback CreateEventCallback(GlobalEventType type)
        {
            MethodInfo eventMethod = GetEventMethodInfo(type.ToString());
            return new EventCallback(type, Delegate.CreateDelegate(typeof(PublishEventDelegate), this, eventMethod) as PublishEventDelegate);
        }
    }

    public delegate void PublishEventDelegate(EventArgsBase args);

    [StructLayout(LayoutKind.Explicit)]
    public struct EventCallback
    {
        [FieldOffset(0)] public GlobalEventType globalType;
        [FieldOffset(0)] public UserEventType userType;
        [FieldOffset(8)] public PublishEventDelegate PublishEvent;

        public EventCallback(UserEventType t, PublishEventDelegate pe)
        {
            globalType = 0;
            userType = t;
            PublishEvent = pe;
        }

        public EventCallback(GlobalEventType t, PublishEventDelegate pe)
        {
            userType = 0;
            globalType = t;
            PublishEvent = pe;
        }
    }

    public class GlobalEventCollection: EventCollection
    {
        // For test purposes
        public event EventHandler<EventArgsBase> Test;

        // TODO in case we need something to be evented...

        public GlobalEventCollection()
        {
            mEvents.Add("GlobalTestArgs", x => ((GlobalEventCollection)x).Test);
        }

        protected List<EventCallback> GenerateEventCallbacks(GlobalEventType type)
        {
            List<EventCallback> callbacks = new();

            if (type == GlobalEventType.None)
                throw new NoEventTypeProvidedException();

            foreach (GlobalEventType t in Enum.GetValues(typeof(GlobalEventType)))
            {
                if ((t & type) != GlobalEventType.None)
                {
                    callbacks.Add(CreateEventCallback(t));
                }
            }

            return callbacks;
        }

        // Register an Event Publisher for Global events.
        // Returns a list of callbacks which should be called to emit an event + for what event type it is for.
        // Returned List will have as many callbacks as there were EventType's provided in @p type
        public List<EventCallback> RegisterEventPublisher(IEventPublisher publisher, GlobalEventType type)
        {
            AddPublisher(publisher);
            return GenerateEventCallbacks(type);
        }
    }

    public class UserEventCollection: EventCollection
    {
        // For test purposes
        public event EventHandler<EventArgsBase> Test;

        public event EventHandler<EventArgsBase> TwitchChatMessage;
        public event EventHandler<EventArgsBase> TwitchChatMessageClear;
        public event EventHandler<EventArgsBase> TwitchChatUserClear;
        public event EventHandler<EventArgsBase> SpotifyMusicStateUpdate;
        public event EventHandler<EventArgsBase> SpotifyMusicTrackChanged;
        public event EventHandler<EventArgsBase> TwitchChannelPointsRedemption;
        public event EventHandler<EventArgsBase> TwitchSubscription;
        public event EventHandler<EventArgsBase> TwitchBitsCheer;

        private string mLBUser;

        public UserEventCollection(string lbUser)
        {
            mLBUser = lbUser;

            mEvents.Add("UserTestArgs", x => ((UserEventCollection)x).Test);
            mEvents.Add("TwitchChatMessageArgs", x => ((UserEventCollection)x).TwitchChatMessage);
            mEvents.Add("TwitchChatMessageClearArgs", x => ((UserEventCollection)x).TwitchChatMessageClear);
            mEvents.Add("TwitchChatUserClearArgs", x => ((UserEventCollection)x).TwitchChatUserClear);
            mEvents.Add("SpotifyMusicStateUpdateArgs", x => ((UserEventCollection)x).SpotifyMusicStateUpdate);
            mEvents.Add("SpotifyMusicTrackChangedArgs", x => ((UserEventCollection)x).SpotifyMusicTrackChanged);
            mEvents.Add("TwitchChannelPointsRedemptionArgs", x => ((UserEventCollection)x).TwitchChannelPointsRedemption);
            mEvents.Add("TwitchSubscriptionArgs", x => ((UserEventCollection)x).TwitchSubscription);
            mEvents.Add("TwitchBitsCheerArgs", x => ((UserEventCollection)x).TwitchBitsCheer);
        }

        protected List<EventCallback> GenerateEventCallbacks(UserEventType type)
        {
            // TODO UserEventType has to be split, figure this thing out please
            List<EventCallback> callbacks = new();

            if (type == UserEventType.None)
                throw new NoEventTypeProvidedException();

            foreach (UserEventType t in Enum.GetValues(typeof(UserEventType)))
            {
                if ((t & type) != UserEventType.None)
                {
                    callbacks.Add(CreateEventCallback(t));
                }
            }

            return callbacks;
        }

        // Register an Event Publisher for User events.
        // Returns a list of callbacks which should be called to emit an event + for what event type it is for.
        // Returned List will have as many callbacks as there were EventType's provided in @p type
        public List<EventCallback> RegisterEventPublisher(IEventPublisher publisher, UserEventType type)
        {
            AddPublisher(publisher);
            return GenerateEventCallbacks(type);
        }
    }

    public class EventSystem
    {
        private Dictionary<string, EventCollection> mUserToCollection = new();


        public EventSystem()
        {
            // for Global events
            mUserToCollection.Add(Common.Constants.LUKEBOT_USER_ID, new GlobalEventCollection());
        }

        ~EventSystem()
        {
        }

        public void AddUser(string lbUser)
        {
            mUserToCollection.Add(lbUser, new UserEventCollection(lbUser));
        }

        public void RemoveUser(string lbUser)
        {
            mUserToCollection.Remove(lbUser);
        }

        public UserEventCollection User(string lbUser)
        {
            return (UserEventCollection)mUserToCollection[lbUser];
        }

        public GlobalEventCollection Global()
        {
            return (GlobalEventCollection)mUserToCollection[Common.Constants.LUKEBOT_USER_ID];
        }
    }
}
