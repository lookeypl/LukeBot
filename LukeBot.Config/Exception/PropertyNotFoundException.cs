namespace LukeBot.Config
{
    public class PropertyNotFoundException: System.Exception
    {
        public PropertyNotFoundException(string name)
            : base(string.Format("Property \"{0}\" not found", name))
        {}
    }
}
