using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LukeBot.Logging;


[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Services
{
    public class Service
    {
        public enum Status
        {
            SHUTDOWN = 0,
            RUNNING,
            SHUTDOWN_FAILED,
        }

        private class ServiceDesc
        {
            public uint serviceId = 0;
            public IService service = null;
            public List<string> dependencyNames = new();
            public List<ServiceDesc> dependencies = new();
            public Status status = Status.SHUTDOWN;
            public bool checkVisited = false;
            public bool visited = false;

            public ServiceDesc(uint id, IService s)
            {
                serviceId = id;
                service = s;
            }
        }

        static private Dictionary<string, ServiceDesc> mServices = new();
        static private LinkedList<ServiceDesc> mServiceStartOrder = new();
        static private uint mServiceCounter = 0;

        static private void RunService(ServiceDesc sd)
        {
            if (sd.status == Status.RUNNING) return;

            foreach (ServiceDesc depsd in sd.dependencies)
            {
                RunService(depsd);
            }

            sd.service.Run();
            sd.status = Status.RUNNING;
        }

        static private void CircularDependencyCheck(ServiceDesc sd)
        {
            try
            {
                if (sd.visited)
                {
                    return;
                }

                if (sd.checkVisited)
                {
                    throw new CircularServiceDependencyChain();
                }

                sd.checkVisited = true;

                foreach (ServiceDesc dep in sd.dependencies)
                {
                    CircularDependencyCheck(dep);
                }

                sd.visited = true;
                sd.checkVisited = false;
                mServiceStartOrder.AddFirst(sd);
            }
            catch (CircularServiceDependencyChain chain)
            {
                chain.Add(sd.service.GetServiceName());
                #pragma warning disable CA2200
                throw chain;
                #pragma warning restore CA2200
            }
        }

        static private void ResolveDependencies()
        {
            foreach (ServiceDesc sd in mServices.Values)
            {
                sd.dependencies = new();

                foreach (string dep in sd.dependencyNames)
                {
                    if (!mServices.ContainsKey(dep))
                        throw new UnresolvedServiceDependencyException(sd.service.GetServiceName(), dep);

                    sd.dependencies.Add(mServices[dep]);
                }
            }

            // check for circular dependencies
            foreach (ServiceDesc sd in mServices.Values)
            {
                try
                {
                    if (!sd.visited) CircularDependencyCheck(sd);
                }
                catch (CircularServiceDependencyChain chain)
                {
                    throw new CircularServiceDependencyException(sd.service.GetServiceName(), chain);
                }
            }
        }

        static public void Run()
        {
            ResolveDependencies();

            foreach (ServiceDesc sd in mServiceStartOrder)
            {
                try
                {
                    RunService(sd);
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to run service {0}: {1} - {2}", sd.service.GetServiceName(), e.GetType().ToString(), e.Message);
                    Logger.Log().Error("Tearing down all running services");
                    Teardown();

                    throw new ServiceRunFailedException(sd.service.GetServiceName(), e);;
                }
            }
        }

        static public void Register(IService service)
        {
            string name = service.GetServiceName();
            if (mServices.ContainsKey(name))
            {
                throw new ServiceAlreadyRegisteredException(name);
            }

            IEnumerable<string> dependencies = service.GetServiceDependencies();

            ServiceDesc sd = new(mServiceCounter, service);
            if (dependencies != null)
            {
                sd.dependencyNames = Enumerable.ToList<string>(dependencies);
            }

            mServices.Add(name, sd);

            mServiceCounter++;
        }

        static public void Unregister(IService service)
        {
            if (mServices.TryGetValue(service.GetServiceName(), out ServiceDesc sd))
            {
                if (sd.service == service)
                {
                    sd.service.RequestShutdown();
                    sd.service.WaitForShutdown();

                    mServices.Remove(service.GetServiceName());
                }
            }
        }

        static public IService Get(string name)
        {
            if (!mServices.ContainsKey(name))
                throw new UnknownServiceException(name);

            return mServices[name].service;
        }

        static public Status GetStatus(string name)
        {
            if (!mServices.ContainsKey(name))
                throw new UnknownServiceException(name);

            return mServices[name].status;
        }

        static public void Teardown()
        {
            foreach (ServiceDesc sd in mServices.Values)
            {
                try
                {
                    if (sd.status == Status.RUNNING)
                    {
                        sd.service.RequestShutdown();
                    }
                }
                catch (System.Exception)
                {
                    sd.status = Status.SHUTDOWN_FAILED;
                }
            }

            foreach (ServiceDesc sd in mServices.Values)
            {
                try
                {
                    if (sd.status == Status.RUNNING)
                    {
                        sd.service.WaitForShutdown();
                        sd.status = Status.SHUTDOWN;
                    }
                }
                catch (System.Exception)
                {
                    sd.status = Status.SHUTDOWN_FAILED;
                }
            }
        }


        // TEST ONLY

        static internal void Cleanup()
        {
            Teardown();
            mServices.Clear();
            mServiceStartOrder.Clear();
        }
    }
}