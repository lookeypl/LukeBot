using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Threading;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Impl;
using Microsoft.AspNetCore.Mvc.Diagnostics;


namespace LukeBot.Tests.Communication.Impl
{
    [TestClass]
    public class EventDispatcherTests
    {
        private const string TEST_EVENT_NAME = "TestEvent";
        private const string TEST_EVENT_DISPATCHER_NAME = "TestDispatcher";
        private const int TEST_VALUE = 42;

        public class TestArgs: EventArgsBase
        {
            public int testValue;
            public AutoResetEvent testDoneEvent = new(false);

            public TestArgs()
                : base("TestArgs")
            {
            }
        }

        private EventDispatcher AllocateDispatcher(EventDispatcherType type)
        {
            switch (type)
            {
                case EventDispatcherType.Immediate: return new ImmediateEventDispatcher(TEST_EVENT_DISPATCHER_NAME);
                case EventDispatcherType.Queued: return new QueuedEventDispatcher(TEST_EVENT_DISPATCHER_NAME);
                case EventDispatcherType.SubscriberQueued: return new SubscriberQueuedEventDispatcher(TEST_EVENT_DISPATCHER_NAME);
                default:
                    Assert.Fail("Invalid Event Dispatcher Type: " + type);
                    return null;
            }
        }

        private void TestDispatcher_Simple(EventDispatcher ed)
        {
            ed.Start();

            Event ev = new(new EventDescriptor()
            {
                Name = TEST_EVENT_NAME,
                Dispatcher = TEST_EVENT_DISPATCHER_NAME
            });

            bool eventSet = false;
            ev.Subscribe((object o, EventArgsBase a) =>
            {
                TestArgs args = a as TestArgs;
                Assert.AreEqual(TEST_VALUE, args.testValue);
                eventSet = true;
                args.testDoneEvent.Set();
                a.Completed();
            });

            TestArgs args = new TestArgs() { testValue = TEST_VALUE };
            ed.Submit(ev, args);
            args.testDoneEvent.WaitOne();
            Assert.AreEqual(true, eventSet);
            Assert.AreEqual(0, ed.Status().EventInfo.Count);

            ed.Stop();
        }

        private void TestDispatcher_Multiple(EventDispatcher ed)
        {
            const int EVENT_COUNT = 20;

            ed.Start();

            Event[] events = new Event[EVENT_COUNT];
            TestArgs[] eventArgs = new TestArgs[EVENT_COUNT];

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                eventArgs[i] = new();

                events[i] = new(new EventDescriptor()
                {
                    Name = TEST_EVENT_NAME + i.ToString(),
                    Dispatcher = TEST_EVENT_DISPATCHER_NAME
                });

                events[i].Subscribe((object o, EventArgsBase a) =>
                {
                    TestArgs args = a as TestArgs;
                    Assert.AreEqual(TEST_VALUE, args.testValue);
                    Thread.Sleep(100); // imitate some "work" to be done
                    args.testDoneEvent.Set();
                    a.Completed();
                });

                eventArgs[i] = new() { testValue = TEST_VALUE };
            }

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                ed.Submit(events[i], eventArgs[i]);
            }

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                eventArgs[i].testDoneEvent.WaitOne();
            }

            Assert.AreEqual(0, ed.Status().EventInfo.Count);
            ed.Stop();
        }

        [TestMethod]
        [DataRow(EventDispatcherType.Immediate)]
        [DataRow(EventDispatcherType.Queued)]
        [DataRow(EventDispatcherType.SubscriberQueued)]
        public void EventDispatcher_Simple(EventDispatcherType type)
        {
            TestDispatcher_Simple(AllocateDispatcher(type));
        }

        [TestMethod]
        [DataRow(EventDispatcherType.Immediate)]
        [DataRow(EventDispatcherType.Queued)]
        [DataRow(EventDispatcherType.SubscriberQueued)]
        public void EventDispatcher_Immediate_Multiple(EventDispatcherType type)
        {
            TestDispatcher_Multiple(AllocateDispatcher(type));
        }
    }
}