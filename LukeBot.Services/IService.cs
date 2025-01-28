using System.Collections.Generic;


namespace LukeBot.Services
{
    public interface IService
    {
        public string GetServiceName();
        public IEnumerable<string> GetServiceDependencies();
        public UserModuleDescriptor GetUserModuleDescriptor();
        public void Run();
        public void RequestShutdown();
        public void WaitForShutdown();
    }
}