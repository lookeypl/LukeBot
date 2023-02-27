namespace LukeBot.CLI
{
    internal class LambdaCommand: Command
    {
        private Interface.CmdDelegate mDelegate;

        public LambdaCommand(Interface.CmdDelegate d)
        {
            mDelegate = d;
        }

        public string Execute(string[] args)
        {
            return mDelegate(args);
        }
    }
}