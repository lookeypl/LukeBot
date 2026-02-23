using System;
using System.Linq;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Logging;


namespace LukeBot.Communication.Impl
{
    public class EventCollection: IEventCollection
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
        public IEvent Event(string name)
        {
            if (!mEvents.ContainsKey(name))
                throw new EventNotFoundException(name);

            return mEvents[name];
        }

        /**
         * Get registered Dispatcher of specified name.
         *
         * This allows access to specific Dispatcher's (and its Events) behavior.
         */
        public EventDispatcher Dispatcher(string name)
        {
            return mDispatchers[name];
        }

        private EventCallback CreateEventCallback(string eventName, string dispatcherName)
        {
            Event ev = Event(eventName) as Event;
            EventDispatcher disp = Dispatcher(dispatcherName);
            return new EventCallback(eventName, (EventArgsBase args) => disp.Submit(ev, args));
        }

        private EventCallback AddEvent(EventDescriptor ed)
        {
            if (ed.Name == null || ed.Name.Length == 0)
                throw new EventDescriptorInvalidException("Event name is missing");

            if (mEvents.ContainsKey(ed.Name))
                throw new EventDescriptorInvalidException(string.Format("{0} event already exists", ed.Name));

            string disp = ed.Dispatcher;
            if (disp == null || disp.Length == 0)
                disp = DEFAULT_DISPATCHER_NAME;

            if (!mDispatchers.ContainsKey(disp))
                throw new EventDescriptorInvalidException(string.Format("{0}: Dispatcher {1} does not exist", ed.Name, disp));

            // These are just warnings to handle non-critical but still useful fields
            if (ed.Description == null || ed.Description.Length == 0)
                Logger.Log().Warning(
                    "{0}: Description field is null or empty. Consider adding event's description " +
                    "for better user interaction.",
                    ed.Name
                );

            if (ed.TestGenerator != null && ed.TestParams == null)
                Logger.Log().Warning(
                    "{0}: Test Generator was provided, but without specifying test parameters. " +
                    "Consider adding a list of accepted test parameters for better parsing and user information.",
                    ed.Name
                );

            mEvents.Add(ed.Name, new Event(ed));

            return CreateEventCallback(ed.Name, disp);
        }


        public List<EventCallback> RegisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetEventPublisherName();

            Logger.Log().Debug("Registering publisher {0}", pubName);

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


        public void UnregisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetEventPublisherName();

            Logger.Log().Debug("Unregistering publisher {0}", pubName);

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
            case EventDispatcherType.SubscriberQueued:
                dispatcher = new SubscriberQueuedEventDispatcher(dispName);
                break;
            default:
                throw new ArgumentException("Invalid event dispatcher type");
            }

            dispatcher.Start();

            mDispatchers.Add(dispName, dispatcher);
        }

        public void RemoveEventDispatcher(string dispName)
        {
            if (!mDispatchers.ContainsKey(dispName))
                return; // was already removed, quietly exit

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


        public EventInfo GetEventInfo(string eventName)
        {
            return (Event(eventName) as Event).GetEventInfo();
        }

        public IEnumerable<EventInfo> ListEvents()
        {
            List<EventInfo> events = new();

            foreach (Event e in mEvents.Values)
            {
                events.Add(e.GetEventInfo());
            }

            return events;
        }

        public IEnumerable<EventDispatcherStatus> GetDispatcherStatuses()
        {
            List<EventDispatcherStatus> ret = new();

            foreach (EventDispatcher d in mDispatchers.Values)
            {
                ret.Add(d.Status());
            }

            return ret;
        }

        private void ValidateEventTestArgs(Event ev, IEnumerable<(string, string)> args)
        {
            if (ev.TestParams == null)
                return; // nothing to validate, let's assume this is a responsibilty of the generator

            foreach ((string a, string v) a in args)
            {
                if (!ev.TestParams.Any(param => param.Name == a.a))
                    throw new InvalidTestArgException(a.a);

                EventTestParam testParam = ev.TestParams.Single(param => param.Name == a.a);
                switch (testParam.Type)
                {
                case EventTestParamType.Integer:
                    // check if we can parse the value to int32
                    if (!Int32.TryParse(a.v, out int result))
                        throw new InvalidTestArgException(a.a);
                    break;
                default:
                    break;
                }
            }
        }

        public void TestEvent(string name, IEnumerable<(string, string)> args)
        {
            Event ev = mEvents[name];

            if (ev.TestGenerator == null)
                throw new TestArgGeneratorMissingException(ev.Name);

            string dispatcher = ev.Dispatcher;
            if (dispatcher == null || dispatcher.Length == 0)
                dispatcher = DEFAULT_DISPATCHER_NAME;

            ValidateEventTestArgs(ev, args);
            mDispatchers[dispatcher].Submit(ev, ev.TestGenerator(args));
        }
    }
}
