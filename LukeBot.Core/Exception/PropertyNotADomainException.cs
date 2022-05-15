namespace LukeBot.Core
{
    public class PropertyNotADomainException: System.Exception
    {
        public PropertyNotADomainException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
