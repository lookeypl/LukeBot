namespace LukeBot.Config
{
    public class PropertyIsADomainException: System.Exception
    {
        public PropertyIsADomainException(string name)
            : base(string.Format("Property {0} is a Domain", name))
        {}
    }
}
