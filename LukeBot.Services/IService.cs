using System;
using System.Collections.Generic;


namespace LukeBot.Services
{
    /**
     * Base class for all Services running in LukeBot.
     *
     * TODO: Turn "Run()" into async call
     * TODO: Turn RequestShutdown()/WaitForShutdown() into async call
     */
    public interface IService
    {
        /**
         * Gets this Service's name.
         *
         * Name is used to recognize a service when calling Service.Get(). Name should be unique
         * compared to other Services, otherwise an error will be returned
         */
        public string GetServiceName();

        /**
         * Gets this Service's dependencies.
         *
         * The list should contain names of Services that this Service depends on. Service
         * manager will make sure those Services are running before calling Run() on this one.
         *
         * If there is a missing dependency - no Service with requested name has been registered -
         * Service manager will throw UnresolvedServiceDependencyException.
         */
        public IEnumerable<string> GetServiceDependencies();

        /**
         * Called by Service manager when this Service is supposed to start. This happens when
         * Service managers' `Service.Run()` was called.
         *
         * Time at which this function is called is only guaranteed at some point after Services
         * listed in GetServiceDependencies() had their Run() method called.
         *
         * Throwing an Exception from this function will stop the entire process.
         */
        public void Run();

        /**
         * Called by Service manager during the first phase of its `Service.Teardown()` call.
         * A first notification that this Service should start shutting itself down.
         *
         * Service should prepare for shutdown at this point and, if necessary, prepare all running
         * Threads to wrap-up and stop.
         */
        public void RequestShutdown();

        /**
         * Called by Service manager during the second phase of its `Service.Teardown()` call.
         * Awaits for Services to fully shut down.
         *
         * Service should wait for any running Threads at this point and make sure that all of them
         * completed their job.
         */
        public void WaitForShutdown();
    }
}