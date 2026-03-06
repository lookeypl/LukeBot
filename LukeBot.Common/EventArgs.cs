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

        public void SetCompletionCallback(CompletionCallback callback)
        {
            Completion = callback;
        }

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

    public abstract class SerializableEventArgsBase: EventArgsBase
    {
        public SerializableEventArgsBase(string name)
            : base(name)
        {}

        public abstract string Serialize();
    }

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
