using LukeBot.Communication.Events;


namespace LukeBot.Communication
{
    public class EventArgsNotFoundException: System.Exception
    {
        public EventArgsNotFoundException(string type)
            : base(string.Format("Not found args for event type: {0}", type))
        {}
    }
}
