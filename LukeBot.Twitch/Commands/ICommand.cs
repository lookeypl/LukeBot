using LukeBot.Common;


namespace LukeBot.Twitch.Command
{
    public interface ICommand
    {
        // Provided args from a chat message; returns a message to send back
        // That way TwitchIRC will
        public string Execute(string[] args);
    }
}
