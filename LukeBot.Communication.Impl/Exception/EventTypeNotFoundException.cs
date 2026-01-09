using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class EventTypeNotFoundException: System.Exception
    {
        public EventTypeNotFoundException(string type)
            : base(string.Format("Not found event type: {0}", type))
        {}
    }
}
