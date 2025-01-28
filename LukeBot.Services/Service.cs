using System.Collections.Generic;
using System.Linq;
using LukeBot.Logging;


namespace LukeBot.Services
{
    public class Service
    {
        private class ServiceDesc
        {
            public IService service = null;
            public List<string> dependencyNames = new();
            public List<IService> dependencies = new();
            public bool initialized = false;

            public ServiceDesc() {}
        }

        static private UserModuleManager mModuleManager = null;
        static private bool mInitialized = false;
        static private Dictionary<string, ServiceDesc> mServices = new();

        static public UserModuleManager ModuleManager
        {
            get
            {
                return mModuleManager;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mModuleManager = new UserModuleManager();

            mInitialized = true;
        }

        static private void RunService(ServiceDesc sd)
        {
            if (sd.initialized) return;

            if (sd.dependencies != null)
            {
                foreach (ServiceDesc depsd in sd.dependencies)
                {
                    RunService(depsd);
                }
            }

            sd.service.Run();
            sd.initialized = true;
        }

        static private void ResolveDependencies()
        {

        }

        static public void Run()
        {
            ResolveDependencies();

            foreach (ServiceDesc sd in mServices.Values)
            {
                try
                {
                    RunService(sd);
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to run service {0}: {1} - {2}", sd.service.GetServiceName(), e.GetType().ToString(), e.Message);
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

            ServiceDesc sd = new();
            sd.service = service;
            sd.dependencyNames = dependencies != null ? Enumerable.ToList<string>(dependencies) : null;

            mServices.Add(name, sd);
        }

        static public IService Get(string name)
        {
            return mServices[name].service;
        }

        static public void Teardown()
        {
            foreach (ServiceDesc sd in mServices.Values)
            {
                sd.service.RequestShutdown();
            }

            foreach (ServiceDesc sd in mServices.Values)
            {
                sd.service.WaitForShutdown();
                sd.initialized = false;
            }

            mInitialized = false;
        }
    }
}