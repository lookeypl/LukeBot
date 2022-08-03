namespace LukeBot.Config
{
    public class PropertyTypeInvalidException: System.Exception
    {
        public PropertyTypeInvalidException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
