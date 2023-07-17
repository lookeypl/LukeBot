namespace LukeBot.Interface
{
    public interface CLI: InterfaceBase
    {
        public delegate string CmdDelegate(string[] args);

        void AddCommand(string cmd, Command c);
        void AddCommand(string cmd, CmdDelegate d);
        void SetPromptPrefix(string prefix);
    }
}