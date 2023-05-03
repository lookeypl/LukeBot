namespace LukeBot.Interface
{
    internal class LambdaCommand: Command
    {
        private CLI.CmdDelegate mDelegate;

        public LambdaCommand(CLI.CmdDelegate d)
        {
            mDelegate = d;
        }

        public string Execute(string[] args)
        {
            return mDelegate(args);
        }
    }
}