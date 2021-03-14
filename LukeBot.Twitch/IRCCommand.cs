namespace LukeBot.Twitch
{
    public enum IRCCommand
    {
        // Stub for variable initialization
        INVALID,

        // Marks numeric response from the server; can be an actual reply or an error
        REPLY,

        // Regular "string" IRC commands
        CAP,
        CLEARCHAT,
        CLEARMSG,
        HOSTTARGET,
        JOIN,
        NOTICE,
        PART,
        PING,
        PRIVMSG,
        RECONNECT,
        ROOMSTATE,
        USERNOTICE,
        USERSTATE,
    }
}
