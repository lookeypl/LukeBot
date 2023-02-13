namespace LukeBot.Common
{
    public interface IModule
    {
        public void Run();
        public void RequestShutdown();
        public void WaitForShutdown();
    }
}
