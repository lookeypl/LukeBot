using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LukeBot.Common
{
    // TODO this needs to be reworked in the future:
    // - EventArgsBase should probably be specific to Communication (LukeBot.API stands in the way for that)
    // - Some better type variants are needed
    public abstract class EventArgsBase
    {
        public delegate void CompletionCallback();

        private CompletionCallback Completion;

        public string EventName { get; private set; }
        public Guid EventID { get; private set; }

        public EventArgsBase(string name)
        {
            EventName = name;
            EventID = Guid.NewGuid();
            Completion = null;
        }

        /**
         * Called by EventDispatcher to set the completion callback.
         *
         * TODO: This must be made internal to LukeBot.Communication.
         */
        public void SetCompletionCallback(CompletionCallback callback)
        {
            Completion = callback;
        }

        /**
         * Trigger the Completion callback held by this EventArgs object.
         *
         * When raising a Completable event (aka. backend subscribed with completable = true - see
         * IEvent for details) the Dispatcher can provide this args object with a Completion
         * callback, which can later be used to notify the Dispatcher that the event has completed.
         * This is done to have some knowledge of the state of lengthier Events via "event status"
         * CLI callback, and it also allows to interrupt/skip currently active events.
         *
         * If an Event receiver subscribes to an Event with completable argument set to true, it is
         * assumed the receiver will eventually call this method to notify the Event processing is
         * over.
         *
         * If this
         */
        public void Completed()
        {
            if (Completion != null)
            {
                Completion();
            }
        }

        public override string ToString()
        {
            return EventName;
        }
    }

    /**
     * Extension of EventArgsBase providing a Serialize() function.
     *
     * Inherit this class if the EventArgs need to eventually be Serialized (ex. for sending
     * via WebSocket)
     */
    public abstract class SerializableEventArgsBase: EventArgsBase
    {
        public SerializableEventArgsBase(string name)
            : base(name)
        {}

        public abstract string Serialize();
    }

    /**
     * EventArgs specialization for Interrupt events.
     */
    public sealed class InterruptEvent: SerializableEventArgsBase
    {
        public Guid EventToInterrupt { get; private set; }

        public InterruptEvent(Guid toInterrupt)
            : base(nameof(InterruptEvent))
        {
            EventToInterrupt = toInterrupt;
        }

        public override string Serialize()
        {
            return JsonSerializer.Serialize<InterruptEvent>(this);
        }
    }
}
