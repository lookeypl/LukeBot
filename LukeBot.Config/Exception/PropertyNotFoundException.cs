namespace LukeBot.Config
{
    public class PropertyNotFoundException: System.Exception
    {
        public PropertyNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
