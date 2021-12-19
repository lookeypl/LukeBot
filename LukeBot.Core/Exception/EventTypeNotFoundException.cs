namespace LukeBot.Core
{
    class EventTypeNotFoundException: System.Exception
    {
        public EventTypeNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
