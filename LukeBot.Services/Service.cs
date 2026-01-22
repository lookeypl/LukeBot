using System;
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
            public IServiceBase service = null;
            public List<string> dependencyNames = new();
            public List<ServiceDesc> dependencies = new();
            public Status status = Status.SHUTDOWN;
            public bool checkVisited = false;
            public bool visited = false;

            public ServiceDesc(uint id, IServiceBase s)
            {
                serviceId = id;
                service = s;
            }
        }

        // we assume there can only be one Service of one type
        // as such, the name will be based on Service's Interface
        // For that reference we fetch the Service's name based on the type name
        // that directly inherited IService. See GetServiceTypeBasedName().
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
                chain.Add(sd.service.GetServiceDebugName());
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
                        throw new UnresolvedServiceDependencyException(sd.service.GetServiceDebugName(), dep);

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
                    throw new CircularServiceDependencyException(sd.service.GetServiceDebugName(), chain);
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
                    Logger.Log().Error("Failed to run service {0}: {1} - {2}", sd.service.GetServiceDebugName(), e.GetType().ToString(), e.Message);
                    Logger.Log().Error("Tearing down all running services");
                    Teardown();

                    throw new ServiceRunFailedException(sd.service.GetServiceDebugName(), e);;
                }
            }
        }

        static public void Register<T>(IService<T> service) where T: IService<T>
        {
            string name = service.RecognizableType.Name;
            if (mServices.ContainsKey(name))
            {
                throw new ServiceAlreadyRegisteredException(name);
            }

            ServiceDesc sd = new(mServiceCounter, service);

            IEnumerable<string> dependencies = sd.service.GetServiceDependencies();
            if (dependencies != null)
            {
                sd.dependencyNames = Enumerable.ToList<string>(dependencies);
            }

            mServices.Add(name, sd);
            mServiceCounter++;
        }

        static public void Unregister<T>(IService<T> service) where T: IService<T>
        {
            if (mServices.TryGetValue(service.GetServiceDebugName(), out ServiceDesc sd))
            {
                if (sd.service == service)
                {
                    sd.service.RequestShutdown();
                    sd.service.WaitForShutdown();

                    mServices.Remove(service.GetServiceDebugName());
                }
            }
        }

        static public T Get<T>()
            where T: IService<T>
        {
            string name = NameOf<T>();
            if (!mServices.ContainsKey(name))
                throw new UnknownServiceException(name);

            return (T)mServices[name].service;
        }

        static public Status GetStatus<T>()
            where T: IService<T>
        {
            string name = NameOf<T>();
            if (!mServices.ContainsKey(name))
                throw new UnknownServiceException(name);

            return mServices[name].status;
        }

        static public string NameOf<T>()
            where T: IService<T>
        {
            return typeof(T).Name;
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