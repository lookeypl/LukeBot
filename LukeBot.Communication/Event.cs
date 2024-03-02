using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LukeBot.Communication.Common;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

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

    public class Event
    {
        /**
         * Event's name
         */
        public string Name;

        /**
         * Dispather's name responsible for raising this event
         */
        public string Dispatcher;

        /**
         * Description of event's purpose
         */
        public string Description;

        /**
         * Event's endpoint.
         *
         * Subscribe to this event handler to be notified when given
         * Event's Publisher raises it.
         */
        public event EventHandler<EventArgsBase> Endpoint;

        /**
         * Event's interrupt endpoint.
         *
         * Subscribe to this event handler to be notified when
         * previously raised (most probably still ongoing) Event is
         * supposed to be interrupted.
         *
         * Note that so far only Queued Dispatcher provides the functionality
         * to emit Interrupt events.
         */
        public event EventHandler<EventArgsBase> InterruptEndpoint;

        /**
         * Generator for test events provided by the publisher.
         *
         * Used to emit test events. Can be null, which disables test event emitting for this Event.
         *
         * Generator is provided with an IEnumerable<string> containing test attributes taken directly from UI.
         * If TestParams is available, Event System will perform a check whether UI-provided
         * attributes are both of valid name and if values can be parsed to desired type.
         * Any further validation is the responsibility of the generator.
         */
        internal TestGeneratorDelegate TestGenerator;

        /**
         * Parameters accepted by the test generator. Event System will use it both
         * for validation before calling TestGenerator, and to display available options
         * to the user.
         *
         * List has to contain all arguments that are valid and accepted by the Generator.
         * Event System will not guarantee that all of them are persent (this is to allow
         * some events like Twitch Subscription Event to have polymorphism implemented per
         * subscription type) but any arguments inputted by the user that are NOT present
         * here will fail the validation.
         */
        internal IEnumerable<EventTestParam> TestParams;



        public Event(EventDescriptor ed)
        {
            Name = ed.Name;
            Dispatcher = ed.Dispatcher;
            Description = ed.Description;
            TestGenerator = ed.TestGenerator;
            TestParams = ed.TestParams;
        }

        internal void Raise(EventArgsBase args)
        {
            if (Endpoint != null)
            {
                Endpoint(this, args);
            }
        }

        internal void Interrupt()
        {
            if (InterruptEndpoint != null)
            {
                InterruptEndpoint(this, null);
            }
        }

        internal EventInfo GetEventInfo()
        {
            return new()
            {
                Name = this.Name,
                Dispatcher = (this.Dispatcher != null && this.Dispatcher.Length > 0) ? this.Dispatcher : "DEFAULT",
                Description = this.Description,
                Testable = this.TestGenerator != null,
                TestParams = this.TestParams
            };
        }
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