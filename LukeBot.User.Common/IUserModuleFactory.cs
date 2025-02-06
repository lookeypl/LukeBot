namespace LukeBot.User.Common
{
    public interface IUserModuleFactory
    {
        public IUserModule CreateModule(IUserContext user);
        public IUserModule GetModule(IUserContext user);
        public void DestroyModule(IUserContext user);
    }
}
