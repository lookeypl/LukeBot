using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Threading;
using LukeBot.Twitch;
using LukeBot.Twitch.Impl;


namespace LukeBot.Tests.Twitch.Impl
{
    /**
     * EventSub tests class.
     *
     * These tests check basic functionalities of EventSub part in LukeBot.Twitch module.
     *
     * Tests *heavily* rely on Twitch CLI tool, which must be present in the system inside PATH.
     * Instructions to download the tool can be found here:
     *   https://github.com/twitchdev/twitch-cli/tree/main#download
     *
     * If Twitch CLI tool is not found in PATH at test runtime all tests will be skipped.
     *
     * Tests can run in two ways, which is chosen automatically by the fixture:
     *  - As-is - Test class will setup its own mock Twitch WebSocket server and use it for testing.
     *    While this method is good for regular day-to-day test runs, it also means server's output
     *    is consumed by the tests.
     *  - With own mock EventSub WebSocket instance - manually-started EventSub WS server can be
     *    used by the fixture. To do that, run following line in a separate CLI window:
     *       `twitch event websocket start-server`
     *    This will start a mock websocket server and output its logs on the console window. Tests
     *    should recognize this (by connecting to default WS server endpoint) and not start its own
     *    instance. Might come in handy when debugging.
     *
     * In both cases Twitch CLI is still required in PATH, as some actions are triggered by tests
     * calling Twitch CLI (ex. reconnect or specific events).
     */
    [TestClass]
    public class EventSubTests: TwitchTestBase
    {
        private enum TwitchWSStatus
        {
            Unknown = 0,
            Own,
            External
        }

        private static Process mTwitchWSProcess;
        private static TwitchWSStatus mTwitchWSStatus = TwitchWSStatus.Unknown;

        private static readonly string EVENT_SUB_TEST_REDEMPTION_USER = "chatter";
        private static readonly string EVENT_SUB_TEST_REDEMPTION_ID = "thisisatestid";
        private static readonly string EVENT_SUB_TEST_REDEMPTION_NAME = "Test reward";
        private static readonly int EVENT_SUB_TEST_REDEMPTION_COST = 420;

        private EventSubClient es = null;

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

        private Process CallTwitchCLI(params string[] args)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = TestMethodSkippedWithoutTwitchCLIAttribute.GetCLIPath();
            foreach (string a in args)
                startInfo.ArgumentList.Add(a);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            Process ret = new Process();
            ret.StartInfo = startInfo;
            ret.Start();

            return ret;
        }

        private async Task EnsureTwitchCLIStarted()
        {
            if (mTwitchWSStatus == TwitchWSStatus.Unknown)
            {
                // check if user is running twitch mock manually
                // if they did, leave like nothing happened - probably they want to see what happens
                // on its side
                try
                {
                    ClientWebSocket socket = new ClientWebSocket();
                    await socket.ConnectAsync(new Uri(TWITCH_MOCK_URI), new CancellationTokenSource(10000).Token);

                    // we connected successfully so the server is already up
                    // setup a fake mTwitchWSProcess to not come back here this test run and leave
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    mTwitchWSStatus = TwitchWSStatus.External;
                    return;
                }
                catch (Exception)
                {
                    // noop, there is no server so we'll setup our own
                }

                mTwitchWSProcess = CallTwitchCLI("event", "websocket", "start-server", "--require-subscription");

                while (true)
                {
                    string line = await mTwitchWSProcess.StandardError.ReadLineAsync();
                    Assert.IsNotNull(line);
                    if (line.Contains("Connect to the WebSocket server at:"))
                        break;
                }

                mTwitchWSStatus = TwitchWSStatus.Own;
            }
        }

        private async Task ConnectEventSub()
        {
            AutoResetEvent connectedEvent = new(false);
            es.Connected += (e, a) =>
            {
                connectedEvent.Set();
            };

            await es.ConnectAsync(null, TWITCH_MOCK_URI);

            Assert.IsTrue(connectedEvent.WaitOne(5 * 1000));
        }

        [ClassInitialize]
        static public void EventSub_Initialize(TestContext context)
        {
            InitializeTestClass();
        }

        [ClassCleanup]
        static public void EventSub_Teardown()
        {
            if (mTwitchWSProcess != null && mTwitchWSStatus == TwitchWSStatus.Own)
            {
                mTwitchWSProcess.Kill();
            }

            CleanupTestClass();
        }

        [TestInitialize]
        public async Task EventSub_InitializeTest()
        {
            if (es != null)
            {
                EventSub_CleanupTest();
            }

            await EnsureTwitchCLIStarted();

            es = new (testUserContext, mockIdentity);
        }

        [TestCleanup]
        public void EventSub_CleanupTest()
        {
            es.RequestShutdown();
            es.WaitForShutdown();
            es = null;
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public void EventSub_Connect()
        {
            es.Connect(null, TWITCH_MOCK_URI);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_ConnectAsync()
        {
            await ConnectEventSub();
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Subscribe()
        {
            await ConnectEventSub();

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_UPDATE);
            es.Subscribe(events);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Reconnect()
        {
            AutoResetEvent notificationReceivedEvent = new(false);
            bool castedSuccessfully = false;
            eventService.User(TWITCH_TEST_USER).Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Subscribe((e, a) =>
            {
                TwitchChannelPointsRedemptionArgs args = a as TwitchChannelPointsRedemptionArgs;
                castedSuccessfully = (args != null);
                Console.Error.WriteLine(String.Format("user: {0} name: {1} title: {2}", args.User, args.DisplayName, args.Title));
                notificationReceivedEvent.Set();
            });

            AutoResetEvent reconnectedEvent = new(false);
            es.Reconnected += (e, args) =>
            {
                reconnectedEvent.Set();
            };

            await ConnectEventSub();

            Assert.AreNotEqual(TwitchWSStatus.Unknown, mTwitchWSStatus);

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            es.Subscribe(events);

            // NOTE - reconnect testing can only happen once every 30 seconds.
            // Technically only one test should call this funciton per run. But,
            // if you're running your own Twitch CLI mock server, make sure to NOT
            // run tests too frequently (there's no way to detect this situation).
            Process reconnectCall = CallTwitchCLI("event", "websocket", "reconnect");
            await reconnectCall.WaitForExitAsync();
            Assert.AreEqual(0, reconnectCall.ExitCode);

            Assert.IsTrue(reconnectedEvent.WaitOne(5 * 1000));

            // test that reconnect went through and subscription still works
            Process eventTriggerCall = CallTwitchCLI(
                "event", "trigger",
                "channel.channel_points_custom_reward_redemption.add",
                "--transport", "websocket",
                "--session", es.SessionID,
                "--item-id", EVENT_SUB_TEST_REDEMPTION_ID,
                "--item-name", EVENT_SUB_TEST_REDEMPTION_NAME,
                "--cost", EVENT_SUB_TEST_REDEMPTION_COST.ToString(),
                "--from-user", EVENT_SUB_TEST_REDEMPTION_USER
            );
            await eventTriggerCall.WaitForExitAsync();
            Assert.AreEqual(0, eventTriggerCall.ExitCode);

            Assert.IsTrue(notificationReceivedEvent.WaitOne(5 * 1000));
            Assert.IsTrue(castedSuccessfully);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Notification()
        {
            AutoResetEvent notificationReceivedEvent = new(false);

            // it's a litt
            bool castedSuccessfully = false;
            bool correctID = false;
            bool correctCost = false;
            bool correctTitle = false;

            eventService.User(TWITCH_TEST_USER).Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Subscribe((e, a) =>
            {
                TwitchChannelPointsRedemptionArgs args = a as TwitchChannelPointsRedemptionArgs;

                Console.Error.WriteLine(String.Format("user: {0} name: {1} title: {2}", args.User, args.DisplayName, args.Title));

                castedSuccessfully = (args != null);
                correctID = (EVENT_SUB_TEST_REDEMPTION_ID == args.NoticeID);
                correctCost = (EVENT_SUB_TEST_REDEMPTION_COST == args.Cost);
                correctTitle = (EVENT_SUB_TEST_REDEMPTION_NAME == args.Title);

                notificationReceivedEvent.Set();
            });

            await ConnectEventSub();

            Assert.AreNotEqual(TwitchWSStatus.Unknown, mTwitchWSStatus);

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            es.Subscribe(events);

            // testing channel point redemption
            Process eventTriggerCall = CallTwitchCLI(
                "event", "trigger",
                "channel.channel_points_custom_reward_redemption.add",
                "--transport", "websocket",
                "--session", es.SessionID,
                "--item-id", EVENT_SUB_TEST_REDEMPTION_ID,
                "--item-name", EVENT_SUB_TEST_REDEMPTION_NAME,
                "--cost", EVENT_SUB_TEST_REDEMPTION_COST.ToString()
            );
            await eventTriggerCall.WaitForExitAsync();
            Assert.AreEqual(0, eventTriggerCall.ExitCode);

            Assert.IsTrue(notificationReceivedEvent.WaitOne(5 * 1000));

            Assert.IsTrue(castedSuccessfully);
            Assert.IsTrue(correctID);
            Assert.IsTrue(correctCost);
            Assert.IsTrue(correctTitle);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        [DataRow(Events.TWITCH_SUBSCRIPTION, EventSubClient.SUB_SUBSCRIBE)]
        public void EventSub_Events(string expectedLBEvent, string receivedESEvent)
        {
            // ...
        }

        [TestMethod]
        public void EventSub_Generators()
        {
            // ...
        }
    }
}
