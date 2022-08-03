namespace LukeBot.Config
{
    public class PropertyAlreadyExistsException: System.Exception
    {
        public PropertyAlreadyExistsException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
