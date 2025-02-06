namespace LukeBot.User.Common
{
    // TODO This isn't really used in other way than having a common interface for Services
    //      User service should hold references to IUserModuleFactories and call CreateModule()
    //      when requested.
    public interface IUserModuleFactory
    {
        public IUserModule CreateModule(IUserContext user);
        public IUserModule GetModule(IUserContext user);
        public void DestroyModule(IUserContext user);
    }
}
