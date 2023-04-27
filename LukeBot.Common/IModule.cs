namespace LukeBot.Common
{
    public interface IModule
    {
        public string GetModuleName();
        public void Run();
        public void RequestShutdown();
        public void WaitForShutdown();
    }
}
