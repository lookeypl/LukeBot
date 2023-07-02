namespace LukeBot.Config
{
    public class PathEmptyException: System.Exception
    {
        public PathEmptyException(): base("Path is empty") {}
    }
}
