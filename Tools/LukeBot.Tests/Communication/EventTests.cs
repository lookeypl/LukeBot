using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Communication;
using LukeBot.Communication.Common;


namespace LukeBot.Tests.Communication
{
    [TestClass]
    public class EventTests
    {
        private const string TEST_EVENT_NAME = "TestEvent1234";
        private const string TEST_EVENT_DISPATCHER_NAME = "TestDispatcher5678";

        public class TestArgs: EventArgsBase
        {
            public TestArgs()
                : base("TestArgs")
            {
            }
        }

        [TestMethod]
        public void Event_Constructor()
        {
            EventDescriptor ed = new()
            {
                Name = TEST_EVENT_NAME,
                Dispatcher = TEST_EVENT_DISPATCHER_NAME
            };

            Event ev = new(ed);
            Assert.AreEqual(TEST_EVENT_NAME, ev.Name);
            Assert.AreEqual(TEST_EVENT_DISPATCHER_NAME, ev.Dispatcher);
        }

        [TestMethod]
        public void Event_Raise()
        {
            Event ev = new (new EventDescriptor()
            {
                Name = TEST_EVENT_NAME,
                Dispatcher = TEST_EVENT_DISPATCHER_NAME
            });

            bool eventRaised = false;
            ev.Endpoint += (object o, EventArgsBase args) =>
            {
                Assert.IsInstanceOfType(o, typeof(Event));
                Assert.IsInstanceOfType(args, typeof(TestArgs));
                eventRaised = true;
            };

            ev.Raise(new TestArgs());
            Assert.IsTrue(eventRaised);
        }
    }
}