namespace LukeBot.Config
{
    public class PropertyNotADomainException: System.Exception
    {
        public PropertyNotADomainException(string name)
            : base(string.Format("Expected property {0} to be a Domain", name))
        {}
    }
}
