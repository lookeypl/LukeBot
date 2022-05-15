namespace LukeBot.Core
{
    public class PropertyNotFoundException: System.Exception
    {
        public PropertyNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
