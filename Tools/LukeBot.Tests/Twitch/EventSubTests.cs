using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LukeBot.Tests.Twitch
{
    [TestClass]
    public class EventSubTests
    {
        private TestContext testContext;
        public TestContext TestContext
        {
            get
            {
                return testContext;
            }
            set
            {
                testContext = value;
            }
        }

        [ClassInitialize]
        static public void EventSub_Initialize(TestContext context)
        {
        }

        /* TODO these tests don't work... but they might serve as EventSub base
        [TestMethod]
        public void PubSub_ConnectTest()
        {
            API.Twitch.GetUserData fakeData = new API.Twitch.GetUserData();
            fakeData.login = fakeData.id = "tests";

            PubSub ps = new PubSub("tests", null, fakeData);
            UriBuilder uriBuilder = new UriBuilder("ws", SERVER_ADDRESS.ToString() + SERVER_ENDPOINT);
            ps.Connect(uriBuilder.Uri);

            ps.RequestShutdown();
            ps.WaitForShutdown();
        }

        [TestMethod]
        public void PubSub_ReconnectTest()
        {
            API.Twitch.GetUserData fakeData = new API.Twitch.GetUserData();
            fakeData.login = fakeData.id = "tests";

            PubSub ps = new PubSub("tests", null, fakeData);
            UriBuilder uriBuilder = new UriBuilder("ws", SERVER_ADDRESS.ToString() + SERVER_ENDPOINT);
            ps.Connect(uriBuilder.Uri);
            testContext.WriteLine("PubSub connected");

            ForceReconnect();

            ps.RequestShutdown();
            ps.WaitForShutdown();
        }
        */

        [ClassCleanup]
        static public void EventSub_Teardown()
        {
        }
    }
}