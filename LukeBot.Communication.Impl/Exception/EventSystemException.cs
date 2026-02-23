using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class EventSystemException: LukeBot.Common.Exception
    {
        public EventSystemException(string msg, params object[] args)
            : base(string.Format(msg, args))
        {}
    }
}
