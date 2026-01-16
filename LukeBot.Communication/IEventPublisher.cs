using System.Collections.Generic;


namespace LukeBot.Communication
{
    /**
     * Interface that event publishers should inherit from. Provides us
     * with necessary information regarding who publishes events.
     */
    public interface IEventPublisher
    {
        public string GetEventPublisherName();
        public List<EventDescriptor> GetEvents(); // TODO IEnumerable?
    }
}
