using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class EventDescriptorInvalidException: System.Exception
    {
        public EventDescriptorInvalidException(string msg)
            : base(string.Format("Event descriptor invalid: {0}", msg))
        {}
    }
}
