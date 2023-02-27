namespace LukeBot.CLI
{
    public interface Command
    {
        public string Execute(string[] args);
    }
}