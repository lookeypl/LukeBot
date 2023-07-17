
namespace LukeBot.Interface
{
    public interface InterfaceBase
    {
        void Message(string msg);
        bool Ask(string msg);
        string Query(string message);
        void MainLoop();
        void Teardown();
    }
}