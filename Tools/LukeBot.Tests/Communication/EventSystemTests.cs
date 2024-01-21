using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Collections.Generic;
using LukeBot.Communication;
using LukeBot.Communication.Common;


namespace LukeBot.Tests.Communication
{
    [TestClass]
    public class EventSystemTests: IEventPublisher
    {
        private const string USER_EVENT_USERNAME = "test";
        private const string USER_EVENT_USERNAME_2 = "test2";
        private const string TEST_EVENT_NAME = "TestEvent";
        private const string TEST_EVENT_TWO_NAME = "TestEventTwo";

        private EventSystem mEventSystem;
        private List<EventDescriptor> mEventsToTest = new();

        public class TestEventArgs: EventArgsBase
        {
            public TestEventArgs()
                : base(TEST_EVENT_NAME)
            {
            }
        }

        public class EventSystemSecondPublisher: IEventPublisher
        {
            private List<EventDescriptor> mEvents = new();

            public EventSystemSecondPublisher(string[] events, string dispatcher)
            {
                foreach (string ev in events)
                {
                    mEvents.Add(new EventDescriptor()
                    {
                        Name = ev,
                        TargetDispatcher = dispatcher
                    });
                }
            }

            public string GetName()
            {
                return "EventSystemSecondPublisher";
            }

            public List<EventDescriptor> GetEvents()
            {
                return mEvents;
            }
        }

        public string GetName()
        {
            return "EventSystemTestPublisher";
        }

        public List<EventDescriptor> GetEvents()
        {
            return mEventsToTest;
        }

        private List<EventCallback> RegisterAndCheckCallbacksGlobal(string[] events, string dispatcher)
        {
            int expectedEventCount = 0;
            mEventsToTest.Clear();

            if (events != null)
            {
                expectedEventCount = events.Length;
                foreach (string ev in events)
                {
                    mEventsToTest.Add(new EventDescriptor()
                    {
                        Name = ev,
                        TargetDispatcher = dispatcher
                    });
                }
            }

            List<EventCallback> cbs = mEventSystem.Global().RegisterPublisher(this);

            Assert.AreEqual(expectedEventCount, cbs.Count);
            for (int i = 0; i < cbs.Count; ++i)
            {
                Assert.IsNotNull(cbs[i]);
                Assert.IsNotNull(cbs[i].PublishEvent);
                Assert.AreEqual(events[0], cbs[i].eventName);
            }

            return cbs;
        }

        private List<EventCallback> RegisterAndCheckCallbacksUser(string user, string[] events, string dispatcher)
        {
            int expectedEventCount = 0;
            mEventsToTest.Clear();

            if (events != null)
            {
                expectedEventCount = events.Length;
                foreach (string ev in events)
                {
                    mEventsToTest.Add(new EventDescriptor()
                    {
                        Name = ev,
                        TargetDispatcher = dispatcher
                    });
                }
            }

            List<EventCallback> cbs = mEventSystem.User(user).RegisterPublisher(this);

            Assert.AreEqual(expectedEventCount, cbs.Count);
            for (int i = 0; i < cbs.Count; ++i)
            {
                Assert.IsNotNull(cbs[i]);
                Assert.IsNotNull(cbs[i].PublishEvent);
                Assert.AreEqual(events[0], cbs[i].eventName);
            }

            return cbs;
        }

        [TestInitialize]
        public void EventSystem_TestStartup()
        {
            mEventSystem = new EventSystem();
            mEventSystem.AddUser(USER_EVENT_USERNAME);
            mEventSystem.AddUser(USER_EVENT_USERNAME_2);
        }

        [TestMethod]
        public void EventSystem_RegisterGlobalSingle()
        {
            RegisterAndCheckCallbacksGlobal(new string[]{ TEST_EVENT_NAME }, null);
        }

        [TestMethod]
        public void EventSystem_RegisterUserSingle()
        {
            RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME, new string[]{ TEST_EVENT_NAME }, "");
            RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME_2, new string[]{ TEST_EVENT_NAME }, "");
        }

        [TestMethod]
        public void EventSystem_RegisterToNone()
        {
            Assert.ThrowsException<NoEventProvidedException>(() => RegisterAndCheckCallbacksGlobal(null, ""));
            Assert.ThrowsException<NoEventProvidedException>(() => RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME, null, ""));
            Assert.ThrowsException<NoEventProvidedException>(() => RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME_2, null, ""));
        }

        [TestMethod]
        public void EventSystem_RegisterDuplicatePublisher()
        {
            // registering the same publisher to the same collection should throw an exception
            RegisterAndCheckCallbacksGlobal(new string[] { TEST_EVENT_NAME }, "");
            Assert.ThrowsException<PublisherAlreadyRegisteredException>(() => RegisterAndCheckCallbacksGlobal(new string[] { TEST_EVENT_NAME }, ""));
        }

        [TestMethod]
        public void EventSystem_RegisterDuplicateEvent()
        {
            // registering two different publishers offering the same event also should fail
            RegisterAndCheckCallbacksGlobal(new string[] { TEST_EVENT_NAME }, "");
            Assert.ThrowsException<EventAlreadyExistsException>(() =>
                mEventSystem.Global().RegisterPublisher(new EventSystemSecondPublisher(new string[] { TEST_EVENT_NAME }, ""))
            );

            // similarly, a different dispatcher name should not matter (events should be exclusive)
            mEventSystem.Global().AddEventDispatcher("test", EventDispatcherType.Immediate);
            Assert.ThrowsException<EventAlreadyExistsException>(() =>
                mEventSystem.Global().RegisterPublisher(new EventSystemSecondPublisher(new string[] { TEST_EVENT_NAME }, "test"))
            );
        }

        [TestMethod]
        public void EventSystem_RegisterDuplicatePublisherForSeparateUser()
        {
            // registering the same publisher for different users should work just fine
        }

        [TestMethod]
        public void EventSystem_RegisterDuplicateEventForSeparateUser()
        {
            // registering two publishers with the same event name but for separate user should work
            RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME, new string[] { TEST_EVENT_NAME }, "");
            mEventSystem.User(USER_EVENT_USERNAME_2).RegisterPublisher(
                new EventSystemSecondPublisher(new string[] { TEST_EVENT_NAME }, "")
            );
        }

        [TestMethod]
        public void EventSystem_Global()
        {
            List<EventCallback> cbs = RegisterAndCheckCallbacksGlobal(new string[] { TEST_EVENT_NAME }, null);

            bool eventFired = false;
            mEventSystem.Global().Event(TEST_EVENT_NAME).Endpoint += (o, args) =>
            {
                Assert.IsInstanceOfType(args, typeof(TestEventArgs));
                eventFired = true;
            };

            cbs[0].PublishEvent(new TestEventArgs());
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void EventSystem_User()
        {
            List<EventCallback> cbs = RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME, new string[] { TEST_EVENT_NAME }, null);

            bool eventFired = false;
            mEventSystem.User(USER_EVENT_USERNAME).Event(TEST_EVENT_NAME).Endpoint += (o, args) =>
            {
                Assert.IsInstanceOfType(args, typeof(TestEventArgs));
                eventFired = true;
            };

            cbs[0].PublishEvent(new TestEventArgs());
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void EventSystem_UserSeparation()
        {
            List<EventCallback> cbs1 = RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME, new string[] { TEST_EVENT_NAME }, null);
            List<EventCallback> cbs2 = RegisterAndCheckCallbacksUser(USER_EVENT_USERNAME_2, new string[] { TEST_EVENT_NAME }, null);

            bool eventFired1 = false;
            bool eventFired2 = false;
            mEventSystem.User(USER_EVENT_USERNAME).Event(TEST_EVENT_NAME).Endpoint += (o, args) =>
            {
                Assert.IsInstanceOfType(args, typeof(TestEventArgs));
                eventFired1 = true;
            };

            mEventSystem.User(USER_EVENT_USERNAME_2).Event(TEST_EVENT_NAME).Endpoint += (o, args) =>
            {
                Assert.IsInstanceOfType(args, typeof(TestEventArgs));
                eventFired2 = true;
            };

            cbs1[0].PublishEvent(new TestEventArgs());
            Assert.IsTrue(eventFired1);
            Assert.IsFalse(eventFired2);

            eventFired1 = false;
            eventFired2 = false;
            cbs2[0].PublishEvent(new TestEventArgs());
            Assert.IsFalse(eventFired1);
            Assert.IsTrue(eventFired2);
        }

        // TODO needs testing:
        // - QueuedDispatcher
        // - multiple Dispatchers
        // - etc etc... there's lots of scenarios to tackle :)

        [TestCleanup]
        public void EventSystem_TestTeardown()
        {
            mEventSystem = null;
        }
    }
}