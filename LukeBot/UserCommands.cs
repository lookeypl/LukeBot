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

    [Verb("remove", HelpText = "Remove user")]
    public class UserRemoveCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to remove")]
        public string Name { get; set; }

        public UserRemoveCommand()
        {
            Name = "";
        }
    }

    [Verb("select", HelpText = "Select user for further commands")]
    public class UserSelectCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to select. Leave empty to deselect.")]
        public string Name { get; set; }

        public UserSelectCommand()
        {
            Name = "";
        }
    }
}