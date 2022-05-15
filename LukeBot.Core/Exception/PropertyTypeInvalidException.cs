namespace LukeBot.Core
{
    public class PropertyTypeInvalidException: System.Exception
    {
        public PropertyTypeInvalidException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
