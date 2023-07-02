namespace LukeBot.Config
{
    public class TypeNotFoundException: System.Exception
    {
        public TypeNotFoundException(System.Type type)
            : this(type.ToString())
        {}

        public TypeNotFoundException(string type)
            : base(string.Format("Type {0} not found", type))
        {}
    }
}
