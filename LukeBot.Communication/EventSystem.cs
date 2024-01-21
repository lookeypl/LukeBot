using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using LukeBot.Common;
using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    /**
     * Interface that event publishers should inherit from. Provides us
     * with necessary information regarding who publishes events.
     */
    public interface IEventPublisher
    {
        public string GetName();
        public List<EventDescriptor> GetEvents();
    }

    /**
     * A collection of events.
     *
     * This class stores all registered publishers, their events and
     * all dispatchers that were added.
     *
     * By default, an Immediate dispatcher is created, which can
     * be referred to by either not providing any target dispatcher
     * name (null or empty), or specifying "DEFAULT" dispatcher.
     *
     * One Publisher can only register once to one specific collection.
     * Events cannot have duplicate names within one collection, even
     * if they are provided by separate publishers. By design, the "evented"
     * end is not aware who is publishing events. However, this restriction
     * only applies within one collection - separate collections can have
     * same-named events provided by same publishers.
     */
    public class EventCollection
    {
        private string mLBUser;
        private Dictionary<string, IEventPublisher> mPublishers = new();
        private Dictionary<string, EventDispatcher> mDispatchers = new();
        private Dictionary<string, Event> mEvents = new();
        private const string DEFAULT_DISPATCHER_NAME = "DEFAULT";

        internal EventCollection(string lbUser)
        {
            mLBUser = lbUser;

            // Every event collection has a default immediate Dispatcher
            // For any more dispatchers, they need to be manually added
            AddEventDispatcher(DEFAULT_DISPATCHER_NAME, EventDispatcherType.Immediate);
        }

        ~EventCollection()
        {
            foreach (EventDispatcher ed in mDispatchers.Values)
            {
                ed.Stop();
            }
        }

        /**
         * Get registered Event of specified name.
         *
         * To subscribe to events use this function and add your delegate to .Endpoint member.
         */
        public Event Event(string name)
        {
            return mEvents[name];
        }

        private EventCallback CreateEventCallback(string eventName)
        {
            Event ev = Event(eventName);
            return new EventCallback(eventName, (EventArgsBase args) => ev.Raise(args));
        }

        private EventCallback AddEvent(EventDescriptor ed)
        {
            if (mEvents.ContainsKey(ed.Name))
                throw new EventAlreadyExistsException(ed.Name);

            string disp = ed.TargetDispatcher;
            if (disp == null || disp.Length == 0)
                disp = DEFAULT_DISPATCHER_NAME;

            if (!mDispatchers.ContainsKey(disp))
                throw new DispatcherNotFoundException(disp);

            mEvents.Add(ed.Name, new Event(ed));

            return CreateEventCallback(ed.Name);
        }

        /**
         * Register a new Publisher in the collection.
         *
         * This call will query the Publisher for its name and events which are meant
         * to be published. See EventDescriptor class for requested information.
         *
         * Publishers MUST have their own unique name, and provided event names must NOT collide
         * with events already registered by other Publishers.
         */
        public List<EventCallback> RegisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetName();

            if (mPublishers.ContainsKey(pubName))
                throw new PublisherAlreadyRegisteredException(pubName);

            List<EventDescriptor> eventsToAdd = p.GetEvents();
            if (eventsToAdd == null || eventsToAdd.Count == 0)
                throw new NoEventProvidedException();

            List<EventCallback> retCallback = new();

            foreach (var ed in eventsToAdd)
            {
                retCallback.Add(AddEvent(ed));
            }

            mPublishers.Add(pubName, p);

            return retCallback;
        }

        /**
         * Unregister a publisher.
         *
         * This will clear any Events associated with a Publisher.
         *
         * If Publisher's name is not found, returns quietly assuming it was already removed
         * or was not registered in the first place.
         */
        public void UnregisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetName();

            if (!mPublishers.ContainsKey(pubName))
                return;

            List<EventDescriptor> events = p.GetEvents();

            if (events != null)
            {
                foreach (EventDescriptor e in events)
                {
                    if (mEvents.ContainsKey(e.Name))
                        mEvents.Remove(e.Name);
                }
            }

            mPublishers.Remove(pubName);
        }

        /**
         * Add a new Event Dispatcher.
         *
         * This will add an Event Dispatcher of specified name and type. For more information
         * on Dispatcher types, see EventDispatcher abstract class and implementations.
         *
         * Added Event Dispatcher MUST have an unique name, even if it's of different type.
         */
        public void AddEventDispatcher(string dispName, EventDispatcherType type)
        {
            EventDispatcher dispatcher = null;

            switch (type)
            {
            case EventDispatcherType.Immediate:
                dispatcher = new ImmediateEventDispatcher(dispName);
                break;
            case EventDispatcherType.Queued:
                dispatcher = new QueuedEventDispatcher(dispName);
                break;
            default:
                throw new ArgumentException("Invalid event dispatcher type");
            }

            dispatcher.Start();

            mDispatchers.Add(dispName, dispatcher);
        }

        /**
         * Remove an Event Dispatcher.
         *
         * This call will stop an existing dispatcher and remove it from collection.
         *
         * Note that there might be events still using this dispatcher. In such situation
         * there will be an EventStillInUseException thrown. It is best to clean Dispatchers only
         * after the Publisher has been unregistered.
         */
        public void RemoveEventDispatcher(string dispName)
        {
            // check if there still are publishers using the Dispatcher
            foreach (Event e in mEvents.Values)
            {
                if (e.Dispatcher == dispName)
                    throw new EventStillInUseException(e.Name, e.Dispatcher);
            }

            // stop the dispatcher; should be blocking
            mDispatchers[dispName].Stop();
            mDispatchers.Remove(dispName);
        }
    }

    /**
     * Event System entry point.
     *
     * This class allows for more sophisticated control over events. The underlying mechanism
     * uses the standard Event Handlers, but allows for some organization in how they're used.
     *
     * EventSystem collects Event Collections. By design, one Event Collection should be assigned
     * to one LukeBot user. In addition to that, there's a special "global" event collection which
     * should contain and manage all LukeBot-wide events.
     */
    public class EventSystem
    {
        private Dictionary<string, EventCollection> mUserToCollection = new();

        public EventSystem()
        {
            // for Global events
            mUserToCollection.Add(Constants.LUKEBOT_USER_ID, new(Constants.LUKEBOT_USER_ID));
        }

        ~EventSystem()
        {
        }

        public void AddUser(string lbUser)
        {
            mUserToCollection.Add(lbUser, new EventCollection(lbUser));
        }

        public void RemoveUser(string lbUser)
        {
            mUserToCollection.Remove(lbUser);
        }

        public EventCollection User(string lbUser)
        {
            return mUserToCollection[lbUser];
        }

        public EventCollection Global()
        {
            return mUserToCollection[Constants.LUKEBOT_USER_ID];
        }
    }
}
