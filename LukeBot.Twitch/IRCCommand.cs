namespace LukeBot.Twitch
{
    public enum IRCCommand
    {
        INVALID,
        UNKNOWN_NUMERIC,
        LOGIN_001,
        LOGIN_002,
        LOGIN_003,
        LOGIN_004,
        LOGIN_375,
        LOGIN_372,
        LOGIN_376,
        UNKNOWN_421,
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
