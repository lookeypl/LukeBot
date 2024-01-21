using System;
using System.Diagnostics;
using LukeBot.Communication.Common;

namespace LukeBot.Communication
{
    /**
     * A descriptor of a single event on queue
     */
    public class EventDescriptor
    {
        public string Name;
        public string TargetDispatcher;
    }

    public class Event
    {
        public string Name;
        public string Dispatcher;
        public event EventHandler<EventArgsBase> Endpoint;

        public Event(EventDescriptor ed)
        {
            Name = ed.Name;
            Dispatcher = ed.TargetDispatcher;
        }

        public void Raise(EventArgsBase args)
        {
            if (Endpoint != null)
            {
                Endpoint(this, args);
            }
        }
    }

    public delegate void PublishEventDelegate(EventArgsBase args);

    public struct EventCallback
    {
        public string eventName;
        public PublishEventDelegate PublishEvent;

        public EventCallback(string name, PublishEventDelegate pe)
        {
            eventName = name;
            PublishEvent = pe;
        }
    }
}