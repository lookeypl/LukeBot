namespace LukeBot.Module
{
    public interface IUserModule
    {
        public void Run();
        public void RequestShutdown();
        public void WaitForShutdown();
        public ModuleType GetModuleType();
    }
}
