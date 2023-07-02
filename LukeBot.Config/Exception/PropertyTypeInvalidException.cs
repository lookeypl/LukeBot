namespace LukeBot.Config
{
    public class PropertyTypeInvalidException: System.Exception
    {
        public PropertyTypeInvalidException(System.Type type)
            : this(type.ToString())
        {}

        public PropertyTypeInvalidException(string type)
            : base(string.Format("Invalid property type {0}", type))
        {}
    }
}
