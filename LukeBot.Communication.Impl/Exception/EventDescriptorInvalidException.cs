using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class EventDescriptorInvalidException: System.Exception
    {
        public EventDescriptorInvalidException(string msg)
            : base(string.Format("Event descriptor invalid: {0}", msg))
        {}
    }
}
