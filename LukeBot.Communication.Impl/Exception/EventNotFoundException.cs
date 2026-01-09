using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class EventNotFoundException: LukeBot.Common.Exception
    {
        public EventNotFoundException(string eventName)
            : base(string.Format("Not found event: {0}", eventName))
        {}
    }
}
