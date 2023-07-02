namespace LukeBot.Config
{
    public class PropertyFileInvalidException: System.Exception
    {
        public PropertyFileInvalidException(string filename)
            : base(string.Format("Property Store file {0} is invalid", filename))
        {}
    }
}
