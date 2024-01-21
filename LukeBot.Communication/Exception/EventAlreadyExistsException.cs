using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class EventAlreadyExistsException: System.Exception
    {
        public EventAlreadyExistsException(string eventName)
            : base(string.Format("Event \"{0}\" was already added", eventName))
        {}
    }
}
