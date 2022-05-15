namespace LukeBot.Core
{
    public class TypeNotFoundException: System.Exception
    {
        public TypeNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
