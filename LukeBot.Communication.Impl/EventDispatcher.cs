using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Common;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Communication.Impl
{
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

        public override void Submit(IEvent ev, EventArgsBase args)
        {
            // immediately execute an event upon submission
            (ev as Event).Raise(args);
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

        public override void Skip(int idx)
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override EventDispatcherStatus Status()
        {
            return new EventDispatcherStatus()
            {
                Name = mName,
                Type = EventDispatcherType.Immediate,
                EventInfo = new List<string>(),
                State = EventDispatcherState.Running
            };
        }
    }

    /**
     * Dispatches events onto a queue, which are executed sequentially by a separate thread.
     *
     * This Dispatcher allows more control over how events are dispatched. Functionalities
     * include:
     *   - Disabling - ignores incoming events and skips them completely
     *   - Holding - stores incoming events but doesn't raise them
     *   - Skipping - interrupts currently processed event
     *   - Clearing - skips current event and clears all queued events
     *
     * Events from this Dispatcher are enqueued and raised via a separate worker thread.
     * This allows to perform some more time-consuming actions in order and without them
     * possibly happening at the same time.
     *
     * Notable use-case is ex. subscription alerts from Twitch, which play back a TTS with
     * subscription message.
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
        private EventDispatcherState mState = EventDispatcherState.Stopped;
        private List<EventQueueItem> mEvents = new();
        private ManualResetEvent mThreadStartedEvent = new(false);
        private ManualResetEvent mQueueAvailableEvent = new(false);
        private object mEventQueueLock = new();

        // accessed only by worker
        private EventQueueItem mCurrentEvent = null;

        public QueuedEventDispatcher(string name)
            : base(name)
        {
            mThread = new Thread(WorkerMain);
            mThread.Name = name + " Event Dispatcher Worker";
        }

        private void EnqueueItem(EventQueueItem item)
        {
            if (mState != EventDispatcherState.Running &&
                mState != EventDispatcherState.OnHold)
                return;

            lock (mEventQueueLock)
            {
                if (mEvents != null)
                    mEvents.Add(item);
            }
        }

        private EventQueueItem DequeueItem()
        {
            if (mState != EventDispatcherState.Running)
                return null;

            EventQueueItem item = null;

            lock (mEventQueueLock)
            {
                if (mEvents.Count > 0)
                {
                    item = mEvents[0];
                    mEvents.RemoveAt(0);
                }
            }

            return item;
        }

        private void InterruptCurrentEvent()
        {
            if (mCurrentEvent == null)
            {
                Logger.Log().Debug("No current event to skip");
                return;
            }

            mCurrentEvent.ev.Interrupt(mCurrentEvent.args.EventID);
        }

        private void WorkerMain()
        {
            mState = EventDispatcherState.Running;
            mThreadStartedEvent.Set();

            while (true)
            {
                try
                {
                    if (mEvents.Count == 0 || mState == EventDispatcherState.OnHold)
                    {
                        mQueueAvailableEvent.Reset();
                        mQueueAvailableEvent.WaitOne();
                    }

                    if (mState == EventDispatcherState.Done)
                        break;

                    mCurrentEvent = DequeueItem();
                    if (mCurrentEvent != null)
                    {
                        mCurrentEvent.ev.Raise(mCurrentEvent.args);
                        mCurrentEvent = null;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Thread was interrupted - handle restoring after skipping current event
                    mCurrentEvent = null;
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Caught exception on {0} Event Dispatcher: {1}", mName, e.Message);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }

            mState = EventDispatcherState.Stopped;
        }

        public override void Submit(IEvent ev, EventArgsBase args)
        {
            EnqueueItem(new EventQueueItem(ev as Event, args));
            mQueueAvailableEvent.Set();
        }

        public override void Start()
        {
            if (mState != EventDispatcherState.Stopped)
                return;

            mThread.Start();
            mThreadStartedEvent.WaitOne();
        }

        public override void Stop()
        {
            if (mState != EventDispatcherState.Done && mState != EventDispatcherState.Stopped)
            {
                mState = EventDispatcherState.Done;
                mQueueAvailableEvent.Set();
                mThread.Join();

                // wrapped in mutexes in case an event is submitted at the same time somehow
                lock (mEventQueueLock)
                {
                    mEvents.Clear();
                    mEvents = null;
                }

                mThreadStartedEvent.Reset();
            }
        }

        public override void Clear()
        {
            lock (mEventQueueLock)
            {
                mEvents.Clear();
            }
        }

        public override void Enable()
        {
            if (mState != EventDispatcherState.Disabled &&
                mState != EventDispatcherState.OnHold)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Enable() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.Running;
            mQueueAvailableEvent.Set();
        }

        public override void Disable()
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Disable() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.Disabled;
            Clear();
        }

        public override void Hold()
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Hold() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.OnHold;
        }

        public override void Skip(int idx)
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Hold() call", mName, mState);
                return;
            }

            if (idx == 0)
            {
                InterruptCurrentEvent();
            }
            else
            {
                // assumes the event was not yet executed (we execute one at a time)
                // so we can safely remove it from the list
                lock (mEventQueueLock)
                {
                    mEvents.RemoveAt(idx);
                }
            }
        }

        public override EventDispatcherStatus Status()
        {
            lock (mEventQueueLock)
            {
                List<string> eventInfo = new();
                foreach (EventQueueItem ev in mEvents)
                {
                    eventInfo.Add(ev.args.ToString());
                }

                return new EventDispatcherStatus()
                {
                    Name = mName,
                    Type = EventDispatcherType.Queued,
                    EventInfo = eventInfo,
                    State = mState
                };
            }
        }
    }

    /**
     * Event Dispatcher implementing similar routines to QueuedEventDispatcher, however
     * also NOT spawning a separate Thread to enqueue the Events. Instead, this Dispatcher
     * relies on an assumption that the Event's Subscriber will manage its own queue of
     * events.
     *
     * Events are dispatched immediately akin to ImmediateEventDispatcher, however
     * it also will hold the list of dispatched Events and expect the Subscribers
     * to eventually report back the Event was completed. This is done to accomodate
     * situations where we need to send long-lasting Events (see Twitch Subscriptions
     * or Cheers which play alerts) to multiple Subscribers at once, yet we still need
     * a possibility to Interrupt those Events or to query them for details.
     *
     * This Dispatcher is thread-safe.
     */
    internal class SubscriberQueuedEventDispatcher : EventDispatcher
    {
        private class SentEventData
        {
            private Event mEvent;
            private EventArgsBase mArgs;
            private int mExpectedCompletions;
            private int mCurrentCompletions;

            public Guid EventID
            {
                get
                {
                    return mArgs.EventID;
                }
            }

            public SentEventData(Event ev, EventArgsBase args)
            {
                mEvent = ev;
                mArgs = args;
                mExpectedCompletions = ev.CompletableSubscriberCount;
                mCurrentCompletions = 0;
            }

            // Increase completion counter.
            // Returns true if all subscribers report back completion.
            // Can throw EventSystemException if too many completions are received
            public bool MarkCompleted()
            {
                mCurrentCompletions++;

                if (mCurrentCompletions > mExpectedCompletions)
                {
                    throw new EventSystemException("Received too many completions for event {0}. This should not have happened.", mEvent.Name);
                }

                return (mCurrentCompletions == mExpectedCompletions);
            }

            public void Interrupt()
            {
                mEvent.Interrupt(mArgs.EventID);
            }

            public override string ToString()
            {
                return String.Format("({0}/{1}) {2}", mCurrentCompletions, mExpectedCompletions, mArgs.ToString());
            }
        }

        private List<SentEventData> mSentEvents = new();
        private object mEventListLock = new();

        private void EventCompletionHandler(SentEventData evData)
        {
            lock (mEventListLock)
            {
                if (evData.MarkCompleted())
                {
                    // all handlers completed, clear this event from the list
                    mSentEvents.Remove(evData);
                }
            }
        }

        public SubscriberQueuedEventDispatcher(string name)
            : base(name)
        {
        }

        public override void Clear()
        {
            lock (mEventListLock)
            {
                // notify each currently processed event that there is an interruption
                foreach (SentEventData ev in mSentEvents)
                {
                    ev.Interrupt();
                }

                // clear current events
                mSentEvents.Clear();
            }
        }

        public override void Enable()
        {
            // noop/TODO?
        }

        public override void Disable()
        {
            // noop/TODO?
        }

        public override void Hold()
        {
            // noop
            Logger.Log().Warning("TODO: Holding events not implemented in SubscriberQueuedEventDispatcher");
        }

        public override void Skip(int idx)
        {
            SentEventData eventToInterrupt = null;

            lock (mEventListLock)
            {
                if (idx < 0 || idx >= mSentEvents.Count)
                {
                    Logger.Log().Warning("{0}: Invalid event idx {1}", mName, idx);
                    return;
                }

                // there is a chance interrupted event will arrive first to execute the
                // Completion Handler. If that is the case, we don't want to be holding the lock.
                // Hold the reference to the interrupted event locally to free the lock.
                eventToInterrupt = mSentEvents[idx];
            }

            // Completion handlers will handle removing the interrupted event from our collection
            eventToInterrupt.Interrupt();
        }

        public override void Start()
        {
            // noop, nothing to start
        }

        public override void Stop()
        {
            // noop, nothing to end
        }

        public override void Submit(IEvent ev, EventArgsBase args)
        {
            Event e = ev as Event;

            if (e.CompletableSubscriberCount > 0)
            {
                SentEventData data = new(e, args);
                args.SetCompletionCallback(() => EventCompletionHandler(data));

                lock (mEventListLock)
                {
                    mSentEvents.Add(data);
                }
            }

            e.Raise(args);
        }

        public override EventDispatcherStatus Status()
        {
            lock (mEventListLock)
            {
                List<string> eventInfo = new();

                foreach (SentEventData sentEvent in mSentEvents)
                {
                    eventInfo.Add(sentEvent.ToString());
                }

                return new EventDispatcherStatus()
                {
                    Name = mName,
                    Type = EventDispatcherType.SubscriberQueued,
                    State = EventDispatcherState.Running,
                    EventInfo = eventInfo,
                };
            }
        }
    }
}
