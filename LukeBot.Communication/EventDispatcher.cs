using System.Collections.Generic;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public enum EventDispatcherType
    {
        Immediate = 0,
        Queued
    }

    internal abstract class EventDispatcher
    {
        protected string mName;

        protected EventDispatcher(string name)
        {
            mName = name;
        }

        public abstract void Submit(Event ev, EventArgsBase args);
        public abstract void Start();
        public abstract void Stop();
        public abstract void Clear();
        public abstract void Enable();
        public abstract void Disable();
        public abstract void Hold();
        public abstract void Skip();
    }

    /**
     * Simple event dispatcher, executing events immediately after their arrival
     * and on the same thread.
     *
     * Useful for short and quick events which require immediate reaction from the
     * receiving side (ex. Spotify's NowPlaying service). Note that in this Dispatcher
     * event execution will block the calling thread until it is completed.
     *
     * Because Events are dispatched immediately on the calling thread, there is no way
     * to control this Dispatcher from UI.
     */
    internal class ImmediateEventDispatcher: EventDispatcher
    {
        public ImmediateEventDispatcher(string name)
            : base(name)
        {
        }

        public override void Submit(Event ev, EventArgsBase args)
        {
            // immediately execute an event upon submission
            ev.Raise(args);
        }

        public override void Start()
        {
            // noop
        }

        public override void Stop()
        {
            // noop
        }

        public override void Clear()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Enable()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Disable()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Hold()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Skip()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

    }

    /**
     * Dispatches events onto a queue, which are executed sequentially by a separate thread.
     *
     * This Dispatcher allows more control over how events are dispatched.
     */
    internal class QueuedEventDispatcher: EventDispatcher
    {
        class EventQueueItem
        {
            public Event ev;
            public EventArgsBase args;

            public EventQueueItem(Event e, EventArgsBase a)
            {
                ev = e;
                args = a;
            }
        }

        private Thread mThread = null;
        private bool mDone = false;
        private Queue<EventQueueItem> mEvents = new();
        private ManualResetEvent mQueueAvailableEvent = new(false);
        private Mutex mEventQueueMutex = new();

        public QueuedEventDispatcher(string name)
            : base(name)
        {
            mThread = new Thread(WorkerMain);
            mThread.Name = name + " Event Dispatcher Worker";
        }

        private void EnqueueItem(EventQueueItem item)
        {
            mEventQueueMutex.WaitOne();

            if (mEvents != null)
                mEvents.Enqueue(item);

            mEventQueueMutex.ReleaseMutex();
        }

        private EventQueueItem DequeueItem()
        {
            mEventQueueMutex.WaitOne();
            EventQueueItem item = mEvents.Dequeue();
            mEventQueueMutex.ReleaseMutex();

            return item;
        }

        private void WorkerMain()
        {
            while (true)
            {
                try
                {
                    if (mEvents.Count == 0)
                    {
                        mQueueAvailableEvent.Reset();
                        mQueueAvailableEvent.WaitOne();
                    }

                    if (mDone)
                        break;

                    EventQueueItem item = DequeueItem();
                    item.ev.Raise(item.args);
                }
                catch (ThreadInterruptedException)
                {
                    // Thread was interrupted - handle restoring after skipping current event
                    // TODO
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Caught exception on {0} Event Dispatcher: {1}", mName, e.Message);
                }
            }
        }

        public override void Submit(Event ev, EventArgsBase args)
        {
            EnqueueItem(new EventQueueItem(ev, args));
            mQueueAvailableEvent.Set();
        }

        public override void Start()
        {
            mThread.Start();
        }

        public override void Stop()
        {
            if (mThread.ThreadState == ThreadState.Running)
            {
                mDone = true;
                mQueueAvailableEvent.Set();
                mThread.Join();

                // wrapped in mutexes in case an event is submitted at the same time somehow
                mEventQueueMutex.WaitOne();
                mEvents.Clear();
                mEvents = null;
                mEventQueueMutex.ReleaseMutex();
            }
        }

        public override void Clear()
        {
            mEventQueueMutex.WaitOne();
            mEvents.Clear();
            mEventQueueMutex.ReleaseMutex();
        }

        public override void Enable()
        {
            // TODO
        }

        public override void Disable()
        {
            // TODO
        }

        public override void Hold()
        {
            // TODO
        }

        public override void Skip()
        {
            // TODO
        }

    }
}