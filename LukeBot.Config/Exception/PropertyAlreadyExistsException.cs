namespace LukeBot.Config
{
    public class PropertyAlreadyExistsException: System.Exception
    {
        public PropertyAlreadyExistsException(string name)
            : base(string.Format("Property {0} already exists", name))
        {}
    }
}
