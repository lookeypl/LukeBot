namespace LukeBot.Config
{
    public class PropertyFileInvalidException: System.Exception
    {
        public PropertyFileInvalidException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
