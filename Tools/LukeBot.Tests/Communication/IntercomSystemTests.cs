using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Communication;
using LukeBot.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;
using Intercom = LukeBot.Communication.Events.Intercom;


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

        class IntercomTestEchoMessage: Intercom::MessageBase
        {
            int value;

            public IntercomTestEchoMessage(int v)
                : base(INTERCOM_TEST_ENDPOINT, INTERCOM_TEST_ECHO_MSG)
            {
                value = v;
            }

            public int GetValue()
            {
                return value;
            }
        };

        class IntercomTestEchoResponse: Intercom::ResponseBase
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

        void IntercomCallback(Intercom::MessageBase m, ref Intercom::ResponseBase r)
        {
            IntercomTestEchoMessage msg = (IntercomTestEchoMessage)m;
            IntercomTestEchoResponse resp = (IntercomTestEchoResponse)r;

            Console.WriteLine("Intercom callback called");
            mIntercomServiceTaskQueue.Enqueue(new IntercomServiceTask(msg, resp));
            mIntercomServiceEvent.Set();
        }

        Intercom::ResponseBase IntercomRespAllocator(Intercom::MessageBase msg)
        {
            switch (msg.Message)
            {
            case INTERCOM_TEST_ECHO_MSG: return new IntercomTestEchoResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new Intercom::ResponseBase();
        }

        public void IntercomServiceThread()
        {
            Intercom::EndpointInfo endpointInfo = new Intercom::EndpointInfo(INTERCOM_TEST_ENDPOINT, IntercomRespAllocator);
            endpointInfo.AddMessage(INTERCOM_TEST_ECHO_MSG, IntercomCallback);
            mIntercom.Register(endpointInfo);

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

            mIntercomServiceReady.WaitOne();
        }

        [TestMethod]
        public void IntercomSystem_Simple()
        {
            const int MSG_VALUE = 15;

            Console.WriteLine("Main thread: Forming message");
            IntercomTestEchoMessage msg = new IntercomTestEchoMessage(MSG_VALUE);

            Console.WriteLine("Main thread: Requesting response to message");
            IntercomTestEchoResponse resp =
                mIntercom.Request<IntercomTestEchoResponse, IntercomTestEchoMessage>(msg);

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