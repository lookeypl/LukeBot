using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class NoEventProvidedException: System.Exception
    {
        public NoEventProvidedException()
            : base(string.Format("No event was provided"))
        {}
    }
}
