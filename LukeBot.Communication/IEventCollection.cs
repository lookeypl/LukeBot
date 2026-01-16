using System.Collections.Generic;


namespace LukeBot.Communication
{
    /**
     * A collection of Events.
     *
     * This interface gives access to all registered publishers, their Events
     * and all Dispatchers that were added.
     *
     * By default an Immediate dispatcher is created, which can
     * be referred to by either not providing any target Dispatcher
     * name (null or empty), or specifying "DEFAULT" Dispatcher.
     *
     * One Publisher can only register once to one specific Collection.
     * Events cannot have duplicate names within one Collection, even
     * if they are provided by separate Publishers. By design, the "notified"
     * end is not aware who is Publishing events. However, this restriction
     * only applies within one Collection - separate Collections can have
     * same-named Events provided by same Publishers.
     */
    public interface IEventCollection
    {
        /**
         * Get registered IEvent of specified name.
         *
         * To subscribe to events use this function and call Subscribe().
         */
        IEvent Event(string name);

        /**
         * Get registered Dispatcher of specified name.
         *
         * This allows access to specific Dispatcher's (and its Events) behavior.
         */
        EventDispatcher Dispatcher(string name);

        /**
         * Register a new Publisher in the collection.
         *
         * This call will query the Publisher for its name and events which are meant
         * to be published. See EventDescriptor class for requested information.
         *
         * Publishers MUST have their own unique name, and provided event names must NOT collide
         * with events already registered by other Publishers.
         */
        List<EventCallback> RegisterPublisher(IEventPublisher p);

        /**
         * Unregister a publisher.
         *
         * This will clear any Events associated with a Publisher.
         *
         * If Publisher's name is not found, returns quietly assuming it was already removed
         * or was not registered in the first place.
         */
        void UnregisterPublisher(IEventPublisher p);

        /**
         * Add a new Event Dispatcher.
         *
         * This will add an Event Dispatcher of specified name and type. For more information
         * on Dispatcher types, see EventDispatcher abstract class and implementations.
         *
         * Added Event Dispatcher MUST have an unique name, even if it's of different type.
         */
        void AddEventDispatcher(string dispName, EventDispatcherType type);

        /**
         * Remove an Event Dispatcher.
         *
         * This call will stop an existing dispatcher and remove it from collection.
         *
         * Note that there might be events still using this dispatcher. In such situation
         * there will be an EventStillInUseException thrown. It is best to clean Dispatchers only
         * after the Publisher has been unregistered.
         */
        void RemoveEventDispatcher(string dispName);

        /**
         * Get information about a specific event
         */
        EventInfo GetEventInfo(string eventName);

        /**
         * List available Events in this EventCollection
         */
        IEnumerable<EventInfo> ListEvents();

        /**
         * Get statuses of available Event Dispatchers
         */
        IEnumerable<EventDispatcherStatus> GetDispatcherStatuses();

        /**
         * Fire a test Event from this Collection. This will submit @p name Event for execution.
         *
         * @p args is validated based on EventTestInfo provided by a Publisher.
         * Note that those args are optional, so if an Event was not submitted
         * with a Test Generator this will throw a TestArgGeneratorMissingException.
         */
        void TestEvent(string name, IEnumerable<(string, string)> args);
    }
}
