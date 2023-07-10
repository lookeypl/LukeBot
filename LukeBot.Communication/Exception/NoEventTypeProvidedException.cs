using LukeBot.Communication.Events;


namespace LukeBot.Communication
{
    public class NoEventTypeProvidedException: System.Exception
    {
        public NoEventTypeProvidedException()
            : base(string.Format("Event type was not provided"))
        {}
    }
}
