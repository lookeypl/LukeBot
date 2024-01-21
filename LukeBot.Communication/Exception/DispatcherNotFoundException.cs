using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class DispatcherNotFoundException: System.Exception
    {
        public DispatcherNotFoundException(string name)
            : base(string.Format("Dispatcher \"{0}\" does not exist", name))
        {}
    }
}
