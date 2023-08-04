using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using LukeBot.Twitch;
using Newtonsoft.Json;
using WebSocketSharp.Server;


namespace LukeBot.Tests.Twitch
{
    [TestClass]
    public class PubSubTests
    {
        static readonly IPAddress SERVER_ADDRESS = IPAddress.Loopback;
        static readonly int SERVER_PORT = 42069;
        static readonly string SERVER_ENDPOINT = "/pubsub";

        static WebSocketServer mServer;

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

        // TO BE CALLED AFTER CONNECTING TO SERVER
        // Assumes we already connected to the Server here
        void ServerSend(PubSubMessage msg)
        {
            mServer.WebSocketServices[SERVER_ENDPOINT].Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        void ForceReconnect()
        {
            testContext.WriteLine("Sending reconnect");
            ServerSend(new PubSubMessage(PubSubMsgType.RECONNECT));

            testContext.WriteLine("Restarting server");
            mServer = new WebSocketServer(SERVER_ADDRESS, SERVER_PORT, false);
            mServer.AddWebSocketService<Util.PubSubBehavior>("/pubsub");
            mServer.Start();

            testContext.WriteLine("Done");
        }

        [ClassInitialize]
        static public void PubSub_Initialize(TestContext context)
        {
            mServer = new WebSocketServer(SERVER_ADDRESS, SERVER_PORT, false);
            mServer.AddWebSocketService<Util.PubSubBehavior>("/pubsub");
            mServer.Start();
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
        static public void PubSub_Teardown()
        {
            mServer.Stop();
            mServer = null;
        }
    }
}