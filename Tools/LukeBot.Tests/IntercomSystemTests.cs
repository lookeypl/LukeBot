using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Core;
using LukeBot.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;


namespace LukeBot.Tests
{
    [TestClass]
    public class IntercomSystemTests
    {
        private const string INTERCOM_TEST_ENDPOINT = "IntercomTest";
        private const string INTERCOM_TEST_ECHO_MSG = "IntercomTestEchoMessage";

        IntercomSystem mIntercom;
        Thread mIntercomService;
        AutoResetEvent mIntercomServiceReady;
        AutoResetEvent mIntercomServiceEvent;
        Queue<IntercomServiceTask> mIntercomServiceTaskQueue;
        bool mIntercomServiceDone;

        class IntercomTestEchoMessage: IntercomMessageBase
        {
            int value;

            public IntercomTestEchoMessage(int v)
                : base(INTERCOM_TEST_ECHO_MSG)
            {
                value = v;
            }

            public int GetValue()
            {
                return value;
            }
        };

        class IntercomTestEchoResponse: IntercomResponseBase
        {
            int value;

            public IntercomTestEchoResponse()
                : base()
            {
                value = 0;
            }

            public void SetValue(int v)
            {
                value = v;
            }

            public int GetValue()
            {
                return value;
            }
        };

        class IntercomServiceTask
        {
            IntercomTestEchoMessage mMessage;
            IntercomTestEchoResponse mResponse;

            public IntercomServiceTask(IntercomTestEchoMessage m, IntercomTestEchoResponse r)
            {
                mMessage = m;
                mResponse = r;
            }

            public void Execute()
            {
                mResponse.SetValue(mMessage.GetValue());
                mResponse.SignalSuccess();
            }
        }

        void IntercomCallback(IntercomMessageBase m, ref IntercomResponseBase r)
        {
            IntercomTestEchoMessage msg = (IntercomTestEchoMessage)m;
            IntercomTestEchoResponse resp = (IntercomTestEchoResponse)r;

            Console.WriteLine("Intercom callback called");
            mIntercomServiceTaskQueue.Enqueue(new IntercomServiceTask(msg, resp));
            mIntercomServiceEvent.Set();
        }

        IntercomResponseBase IntercomRespAllocator(IntercomMessageBase msg)
        {
            switch (msg.Message)
            {
            case INTERCOM_TEST_ECHO_MSG: return new IntercomTestEchoResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new IntercomResponseBase();
        }

        public void IntercomServiceThread()
        {
            IntercomEndpointInfo endpointInfo = new IntercomEndpointInfo(IntercomRespAllocator);
            endpointInfo.AddMessage(INTERCOM_TEST_ECHO_MSG, IntercomCallback);
            mIntercom.Register(INTERCOM_TEST_ENDPOINT, endpointInfo);

            Console.WriteLine("Service thread: Ready");
            mIntercomServiceReady.Set();
            Console.WriteLine("Service thread: Awaiting shutdown");

            while (!mIntercomServiceDone)
            {
                mIntercomServiceEvent.WaitOne();

                if (mIntercomServiceTaskQueue.TryDequeue(out IntercomServiceTask task))
                {
                    Thread.Sleep(2000);
                    task.Execute();
                }
            }

            Console.WriteLine("Service thread: Shutdown");
        }

        [TestInitialize]
        public void IntercomSystem_TestStartup()
        {
            Console.WriteLine("Startup");
            mIntercom = new IntercomSystem();
            mIntercomService = new Thread(IntercomServiceThread);
            mIntercomServiceReady = new AutoResetEvent(false);
            mIntercomServiceEvent = new AutoResetEvent(false);
            mIntercomServiceTaskQueue = new Queue<IntercomServiceTask>();
            mIntercomServiceDone = false;

            Console.WriteLine("Main thread: Starting service thread");
            mIntercomService.Start();
        }

        [TestMethod]
        public void IntercomSystem_Simple()
        {
            const int MSG_VALUE = 15;

            Console.WriteLine("Main thread: Forming message");
            IntercomTestEchoMessage msg = new IntercomTestEchoMessage(MSG_VALUE);

            Console.WriteLine("Main thread: Requesting response to message");
            IntercomTestEchoResponse resp =
                mIntercom.Request<IntercomTestEchoMessage, IntercomTestEchoResponse>(INTERCOM_TEST_ENDPOINT, msg);

            Console.WriteLine("Main thread: Awaiting for response");
            resp.Wait();
            Console.WriteLine("Main thread: Checking result");
            Assert.AreEqual(MSG_VALUE, resp.GetValue());
            Console.WriteLine("Main thread: Done");
        }

        [TestCleanup]
        public void IntercomSystem_TestTeardown()
        {
            Console.WriteLine("Cleanup");
            mIntercomServiceDone = true;
            mIntercomServiceEvent.Set();
            mIntercomService.Join();
            Console.WriteLine("Cleanup done");
        }
    }
}