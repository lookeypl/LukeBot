namespace LukeBot.Config
{
    public class PropertyFileUnsupportedException: System.Exception
    {
        public PropertyFileUnsupportedException(int version)
            : base(string.Format("Property Store file version {0} is not supported", version))
        {}
    }
}
