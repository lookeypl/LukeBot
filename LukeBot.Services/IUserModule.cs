namespace LukeBot.Services
{
    public interface IUserModule
    {
        public void Run();
        public void RequestShutdown();
        public void WaitForShutdown();
        public string GetModuleType();
    }
}
