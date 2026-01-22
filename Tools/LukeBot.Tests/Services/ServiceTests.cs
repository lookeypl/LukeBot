using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Services;
using System.Collections.Generic;


namespace LukeBot.Tests.Services
{
    [TestClass]
    public class ServiceTests
    {
        private static readonly string SIMPLE_TEST_SERVICE_NAME = "SimpleTestService";
        private static readonly string DEPENDENT_TEST_SERVICE_NAME = "DependentTestService";
        private static readonly string DOUBLE_DEPENDENT_TEST_SERVICE_NAME = "DoubleDependentTestService";
        private static readonly string CIRCULAR_DEPENDENT_A_TEST_SERVICE_NAME = "CircularDependentATestService";
        private static readonly string CIRCULAR_DEPENDENT_B_TEST_SERVICE_NAME = "CircularDependentBTestService";
        private static readonly string CIRCULAR_DEPENDENT_C_TEST_SERVICE_NAME = "CircularDependentCTestService";

        private static readonly string RUN_EXCEPTION_TEST_SERVICE_NAME = "RunExceptionTestService";
        private static readonly string REQUEST_SHUTDOWN_EXCEPTION_TEST_SERVICE_NAME = "RequestShutdownExceptionTestService";
        private static readonly string WAIT_FOR_SHUTDOWN_EXCEPTION_TEST_SERVICE_NAME = "WaitForShutdownExceptionTestService";

        private interface ITestInterfaceService: IService<ITestInterfaceService>
        {
            void TestInterfaceServiceMethod(); // specific test interface
        }

        private class TestInterfaceService: ITestInterfaceService
        {
            public void TestInterfaceServiceMethod()
            {
                // noop for testing
            }

            public IEnumerable<string> GetServiceDependencies()
            {
                return new List<string>();
            }

            public string GetServiceDebugName()
            {
                return "testinterface";
            }

            public void RequestShutdown()
            {
                // noop
            }

            public void Run()
            {
                // noop
            }

            public void WaitForShutdown()
            {
                // noop
            }
        }

        private abstract class TestService<T>: IService<T> where T: TestService<T>
        {
            public bool Running { get; private set; } = false;
            public bool Shutdown { get; private set; } = false;

            public abstract IEnumerable<string> GetServiceDependencies();
            public abstract string GetServiceDebugName();

            public virtual void Run()
            {
                Running = true;
            }

            public virtual void RequestShutdown()
            {
                Running = false;
            }

            public virtual void WaitForShutdown()
            {
                Shutdown = true;
            }
        }

        private class SimpleTestService: TestService<SimpleTestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return null;
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.SIMPLE_TEST_SERVICE_NAME;
            }
        }

        private class DependentTestService: TestService<DependentTestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> {
                    Service.NameOf<SimpleTestService>()
                };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.DEPENDENT_TEST_SERVICE_NAME;
            }
        }

        private class DoubleDependentTestService: TestService<DoubleDependentTestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> {
                    Service.NameOf<SimpleTestService>(),
                    Service.NameOf<DependentTestService>()
                };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.DOUBLE_DEPENDENT_TEST_SERVICE_NAME;
            }
        }

        private class RunExceptionTestService: TestService<RunExceptionTestService>
        {
            public class RunException: System.Exception
            {
                public RunException(string msg)
                    : base(msg)
                {}
            }

            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> { Service.NameOf<SimpleTestService>() };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.RUN_EXCEPTION_TEST_SERVICE_NAME;
            }

            public override void Run()
            {
                throw new RunException("I'm designed to mess up the Run() call!");
            }
        }

        private class RequestShutdownExceptionTestService: TestService<RequestShutdownExceptionTestService>
        {
            public class RequestShutdownException: System.Exception
            {
                public RequestShutdownException(string msg)
                    : base(msg)
                {}
            }

            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> { Service.NameOf<SimpleTestService>() };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.REQUEST_SHUTDOWN_EXCEPTION_TEST_SERVICE_NAME;
            }

            public override void RequestShutdown()
            {
                throw new RequestShutdownException("I'm designed to mess up the RequestShutdown() call!");
            }
        }

        private class WaitForShutdownExceptionTestService: TestService<WaitForShutdownExceptionTestService>
        {
            public class WaitForShutdownException: System.Exception
            {
                public WaitForShutdownException(string msg)
                    : base(msg)
                {}
            }

            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> { Service.NameOf<SimpleTestService>() };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.WAIT_FOR_SHUTDOWN_EXCEPTION_TEST_SERVICE_NAME;
            }

            public override void WaitForShutdown()
            {
                throw new WaitForShutdownException("I'm designed to mess up the WaitForShutdown() call!");
            }
        }

        private class CircularDependentATestService: TestService<CircularDependentATestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> {
                    Service.NameOf<SimpleTestService>(),
                    Service.NameOf<CircularDependentBTestService>()
                };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.CIRCULAR_DEPENDENT_A_TEST_SERVICE_NAME;
            }
        }

        private class CircularDependentBTestService: TestService<CircularDependentBTestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> {
                    Service.NameOf<SimpleTestService>(),
                    Service.NameOf<CircularDependentCTestService>()
                };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.CIRCULAR_DEPENDENT_B_TEST_SERVICE_NAME;
            }
        }

        private class CircularDependentCTestService: TestService<CircularDependentCTestService>
        {
            public override IEnumerable<string> GetServiceDependencies()
            {
                return new List<string> {
                    Service.NameOf<SimpleTestService>(),
                    Service.NameOf<CircularDependentATestService>()
                };
            }

            public override string GetServiceDebugName()
            {
                return ServiceTests.CIRCULAR_DEPENDENT_C_TEST_SERVICE_NAME;
            }
        }


        private void AssertServiceRegistered<T>(T s) where T: TestService<T>
        {
            Assert.IsNotNull(Service.Get<T>());
            Assert.AreEqual(Service.Status.SHUTDOWN, Service.GetStatus<T>());
            Assert.IsFalse(s.Running);
            Assert.IsFalse(s.Shutdown);
        }

        private void AssertServiceRunning<T>(T s) where T: TestService<T>
        {
            Assert.AreEqual(Service.Status.RUNNING, Service.GetStatus<T>());
            Assert.IsTrue(s.Running);
            Assert.IsFalse(s.Shutdown);
        }

        private void AssertServiceShutdown<T>(T s) where T: TestService<T>
        {
            Assert.AreEqual(Service.Status.SHUTDOWN, Service.GetStatus<T>());
            Assert.IsFalse(s.Running);
            Assert.IsTrue(s.Shutdown);
        }


        [TestInitialize]
        public void Service_TestInitialize()
        {
            Service.Cleanup();
        }


        [TestMethod]
        public void Service_Register()
        {
            Service.Register(new SimpleTestService());

            Assert.IsNotNull(Service.Get<SimpleTestService>());
            Assert.AreEqual(Service.Status.SHUTDOWN, Service.GetStatus<SimpleTestService>());
        }

        [TestMethod]
        public void Service_Register_Duplicate()
        {
            SimpleTestService service = new();
            Service.Register(service);

            Assert.ThrowsException<ServiceAlreadyRegisteredException>(() =>
                Service.Register(service)
            );

            // even a separate instance of the same name should raise an exception
            Assert.ThrowsException<ServiceAlreadyRegisteredException>(() =>
                Service.Register(new SimpleTestService())
            );
        }

        [TestMethod]
        public void Service_Unregister()
        {
            SimpleTestService service = new();

            Service.Register(service);
            AssertServiceRegistered(service);

            Service.Unregister(service);
            Assert.ThrowsException<UnknownServiceException>(() => Service.Get<SimpleTestService>());
            Assert.ThrowsException<UnknownServiceException>(() => Service.GetStatus<SimpleTestService>());
        }

        [TestMethod]
        public void Service_Unregister_NotRegistered()
        {
            SimpleTestService service = new();

            Service.Register(service);
            AssertServiceRegistered(service);

            Service.Unregister(service);

            // should not throw any exceptions
            Service.Unregister(service);
        }

        [TestMethod]
        public void Service_Run_Simple()
        {
            SimpleTestService service = new();

            Service.Register(service);
            AssertServiceRegistered(service);

            Service.Run();
            AssertServiceRunning(service);
        }

        [TestMethod]
        public void Service_Run_Dependencies()
        {
            SimpleTestService simple = new();
            DependentTestService dependent = new();
            DoubleDependentTestService doubleDependent = new();

            Service.Register(dependent);
            Service.Register(doubleDependent);
            Service.Register(simple);

            Service.Run();

            AssertServiceRunning(simple);
            AssertServiceRunning(dependent);
            AssertServiceRunning(doubleDependent);
        }

        [TestMethod]
        public void Service_Run_MissingDependency()
        {
            DependentTestService dependent = new();
            DoubleDependentTestService doubleDependent = new();

            Service.Register(dependent);
            Service.Register(doubleDependent);

            Assert.ThrowsException<UnresolvedServiceDependencyException>(() =>
                Service.Run()
            );

            // we shouldn't even attempt actually running the services when there's a missing
            // dependency so assume they are still in "just registered" state
            AssertServiceRegistered(dependent);
            AssertServiceRegistered(doubleDependent);
        }

        [TestMethod]
        public void Service_Run_CircularDependency()
        {
            SimpleTestService simple = new();
            CircularDependentATestService circularA = new();
            CircularDependentBTestService circularB = new();
            CircularDependentCTestService circularC = new();

            Service.Register(circularA);
            Service.Register(circularB);
            Service.Register(circularC);
            Service.Register(simple);
            AssertServiceRegistered(circularA);
            AssertServiceRegistered(circularB);
            AssertServiceRegistered(circularC);
            AssertServiceRegistered(simple);

            CircularServiceDependencyException ex = Assert.ThrowsException<CircularServiceDependencyException>(() =>
                Service.Run()
            );

            // we shouldn't even attempt actually running the services when there's a circular
            // dependency so assume they are still in "just registered" state
            AssertServiceRegistered(circularA);
            AssertServiceRegistered(circularB);
            AssertServiceRegistered(circularC);
            AssertServiceRegistered(simple);
        }

        [TestMethod]
        public void Service_Teardown_NoRun()
        {
            SimpleTestService service = new();

            Service.Register(service);
            AssertServiceRegistered(service);

            Service.Teardown();
            // our Service should still be as if still-registered because it was NOT running
            // aka. no IService interfaces were called
            AssertServiceRegistered(service);
        }

        [TestMethod]
        public void Service_Lifecycle_Normal()
        {
            SimpleTestService service = new();
            DependentTestService dependent = new();

            Service.Register(service);
            Service.Register(dependent);
            AssertServiceRegistered(service);
            AssertServiceRegistered(dependent);

            Service.Run();
            AssertServiceRunning(service);
            AssertServiceRunning(dependent);

            Service.Teardown();
            AssertServiceShutdown(service);
            AssertServiceShutdown(dependent);
        }

        [TestMethod]
        public void Service_Exception_Run()
        {
            SimpleTestService service = new();
            RunExceptionTestService runException = new();

            Service.Register(service);
            Service.Register(runException);

            AssertServiceRegistered(service);
            AssertServiceRegistered(runException);

            // RunException should be caught and passed along as ex.InnerException
            ServiceRunFailedException ex = Assert.ThrowsException<ServiceRunFailedException>(() => Service.Run());
            Assert.IsNotNull(ex.InnerException);
            Assert.IsInstanceOfType(ex.InnerException, typeof(RunExceptionTestService.RunException));

            // Check how Services are doing
            AssertServiceShutdown(service);
            AssertServiceRegistered(runException);
        }

        [TestMethod]
        public void Service_Exception_RequestShutdown()
        {
            SimpleTestService service = new();
            RequestShutdownExceptionTestService requestShutdownException = new();

            Service.Register(service);
            Service.Register(requestShutdownException);

            AssertServiceRegistered(service);
            AssertServiceRegistered(requestShutdownException);

            Service.Run();
            AssertServiceRunning(service);
            AssertServiceRunning(requestShutdownException);

            Service.Teardown();
            AssertServiceShutdown(service);
            Assert.AreEqual(Service.Status.SHUTDOWN_FAILED, Service.GetStatus<RequestShutdownExceptionTestService>());
        }

        [TestMethod]
        public void Service_Exception_WaitForShutdown()
        {
            SimpleTestService service = new();
            WaitForShutdownExceptionTestService waitForShutdownException = new();

            Service.Register(service);
            Service.Register(waitForShutdownException);

            AssertServiceRegistered(service);
            AssertServiceRegistered(waitForShutdownException);

            Service.Run();
            AssertServiceRunning(service);
            AssertServiceRunning(waitForShutdownException);

            Service.Teardown();
            AssertServiceShutdown(service);
            Assert.AreEqual(Service.Status.SHUTDOWN_FAILED, Service.GetStatus<WaitForShutdownExceptionTestService>());
        }

        [TestMethod]
        public void Service_Get()
        {
            Service.Register(new SimpleTestService());
            Service.Register(new TestInterfaceService());

            Assert.IsNotNull(Service.Get<SimpleTestService>());
            Assert.IsNotNull(Service.Get<ITestInterfaceService>());
        }
    }
}
