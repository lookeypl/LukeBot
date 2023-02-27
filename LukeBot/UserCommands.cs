using CommandLine;

namespace LukeBot
{
    [Verb("add", HelpText = "Add user")]
    public class UserAddCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to add")]
        public string Name { get; set; }

        public UserAddCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available users")]
    public class UserListCommand
    {

    }
}