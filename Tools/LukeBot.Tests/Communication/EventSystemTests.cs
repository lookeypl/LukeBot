using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using LukeBot.Communication;
using LukeBot.Communication.Events;


namespace LukeBot.Tests.Communication
{
    [TestClass]
    public class EventSystemTests: IEventPublisher
    {
        private const string USER_EVENT_USERNAME = "test";
        private const string USER_EVENT_USERNAME_2 = "test2";

        EventSystem mEventSystem;

        private List<EventCallback> RegisterAndCheckCallbacks(GlobalEventType type)
        {
            int expectedEventCount = 0;
            foreach (GlobalEventType t in Enum.GetValues(typeof(GlobalEventType)))
            {
                if ((t & type) != GlobalEventType.None)
                {
                    expectedEventCount++;
                }
            }

            List<EventCallback> cbs = mEventSystem.Global().RegisterEventPublisher(this, type);

            Assert.AreEqual(expectedEventCount, cbs.Count);
            foreach (EventCallback cb in cbs)
            {
                Assert.IsNotNull(cb);
                Assert.AreNotEqual(GlobalEventType.None, cb.userType);
                Assert.IsNotNull(cb.PublishEvent);
            }

            return cbs;
        }

        private List<EventCallback> RegisterAndCheckCallbacks(string user, UserEventType type)
        {
            int expectedEventCount = 0;
            foreach (UserEventType t in Enum.GetValues(typeof(UserEventType)))
            {
                if ((t & type) != UserEventType.None)
                {
                    expectedEventCount++;
                }
            }

            List<EventCallback> cbs = mEventSystem.User(user).RegisterEventPublisher(this, type);

            Assert.AreEqual(expectedEventCount, cbs.Count);
            foreach (EventCallback cb in cbs)
            {
                Assert.IsNotNull(cb);
                Assert.AreNotEqual(UserEventType.None, cb.userType);
                Assert.IsNotNull(cb.PublishEvent);
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
            RegisterAndCheckCallbacks(GlobalEventType.GlobalTest);
        }

        [TestMethod]
        public void EventSystem_RegisterUserSingle()
        {
            RegisterAndCheckCallbacks(USER_EVENT_USERNAME, UserEventType.UserTest);
            RegisterAndCheckCallbacks(USER_EVENT_USERNAME_2, UserEventType.UserTest);
        }

        [TestMethod]
        public void EventSystem_RegisterToNone()
        {
            Assert.ThrowsException<NoEventTypeProvidedException>(() => RegisterAndCheckCallbacks(GlobalEventType.None));
            Assert.ThrowsException<NoEventTypeProvidedException>(() => RegisterAndCheckCallbacks(USER_EVENT_USERNAME, UserEventType.None));
            Assert.ThrowsException<NoEventTypeProvidedException>(() => RegisterAndCheckCallbacks(USER_EVENT_USERNAME_2, UserEventType.None));
        }

        [TestMethod]
        public void EventSystem_RegisterToMultiple()
        {
            RegisterAndCheckCallbacks(USER_EVENT_USERNAME,
                UserEventType.UserTest | UserEventType.TwitchChatMessage | UserEventType.SpotifyMusicStateUpdate | UserEventType.TwitchChatMessageClear
            );
        }

        [TestMethod]
        public void EventSystem_Global()
        {
            List<EventCallback> cbs = RegisterAndCheckCallbacks(GlobalEventType.GlobalTest);

            bool eventFired = false;
            mEventSystem.Global().Test += (o, args) =>
            {
                eventFired = true;
            };

            cbs[0].PublishEvent(new GlobalTestArgs());
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void EventSystem_User()
        {
            List<EventCallback> cbs = RegisterAndCheckCallbacks(USER_EVENT_USERNAME, UserEventType.UserTest);

            bool eventFired = false;
            mEventSystem.User(USER_EVENT_USERNAME).Test += (o, args) =>
            {
                eventFired = true;
            };

            cbs[0].PublishEvent(new UserTestArgs());
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void EventSystem_UserSeparation()
        {
            List<EventCallback> cbs1 = RegisterAndCheckCallbacks(USER_EVENT_USERNAME, UserEventType.UserTest);
            List<EventCallback> cbs2 = RegisterAndCheckCallbacks(USER_EVENT_USERNAME_2, UserEventType.UserTest);

            bool eventFired1 = false;
            bool eventFired2 = false;
            mEventSystem.User(USER_EVENT_USERNAME).Test += (o, args) =>
            {
                eventFired1 = true;
            };

            mEventSystem.User(USER_EVENT_USERNAME_2).Test += (o, args) =>
            {
                eventFired2 = true;
            };

            cbs1[0].PublishEvent(new UserTestArgs());
            Assert.IsTrue(eventFired1);
            Assert.IsFalse(eventFired2);

            eventFired1 = false;
            eventFired2 = false;
            cbs2[0].PublishEvent(new UserTestArgs());
            Assert.IsFalse(eventFired1);
            Assert.IsTrue(eventFired2);
        }

        [TestCleanup]
        public void EventSystem_TestTeardown()
        {
            mEventSystem = null;
        }
    }
}