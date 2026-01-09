using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class EventStillInUseException: System.Exception
    {
        public EventStillInUseException(string eventName, string dispatcher)
            : base(string.Format("Event \"{0}\" is still in use by dispatcher \"{1}\"", eventName, dispatcher))
        {}
    }
}
