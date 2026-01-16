using LukeBot.Common;


namespace LukeBot.Communication
{
    public enum EventDispatcherType
    {
        Immediate = 0,
        Queued
    }

    public enum EventDispatcherState
    {
        Stopped = 0,
        Running,
        OnHold,
        Disabled,
        Done
    }

    public struct EventDispatcherStatus
    {
        public EventDispatcherType Type;
        public string Name;
        public EventDispatcherState State;
        public int EventCount;
    }

    public abstract class EventDispatcher
    {
        protected string mName;

        protected EventDispatcher(string name)
        {
            mName = name;
        }

        public abstract void Submit(IEvent ev, EventArgsBase args);
        public abstract void Start();
        public abstract void Stop();
        public abstract void Clear();
        public abstract void Enable();
        public abstract void Disable();
        public abstract void Hold();
        public abstract void Skip();
        public abstract EventDispatcherStatus Status();
    }
}
