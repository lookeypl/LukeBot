namespace LukeBot.Core
{
    public class PropertyFileInvalidException: System.Exception
    {
        public PropertyFileInvalidException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
