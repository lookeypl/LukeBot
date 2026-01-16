using System;
using System.Collections.Generic;
using LukeBot.Common;


namespace LukeBot.Communication
{
    public enum EventTestParamType
    {
        String,
        Integer
    }

    public class EventTestParam
    {
        public string Name;
        public string Description;
        public EventTestParamType Type;
    }

    /**
     * A descriptor of a single event on queue
     */
    public class EventDescriptor
    {
        public string Name;
        public string Dispatcher;
        public string Description;
        public TestGeneratorDelegate TestGenerator;
        public IEnumerable<EventTestParam> TestParams;
    }

    /**
     * An Info struct for UI and such to get more information about the Event
     */
    public class EventInfo
    {
        public string Name;
        public string Dispatcher;
        public string Description;
        public bool Testable;
        public IEnumerable<EventTestParam> TestParams;
    }

    public delegate void PublishEventDelegate(EventArgsBase args);
    public delegate EventArgsBase TestGeneratorDelegate(IEnumerable<(string, string)> args);

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