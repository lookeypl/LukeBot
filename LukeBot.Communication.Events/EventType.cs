using System;


namespace LukeBot.Communication.Events
{
    [Flags]
    public enum UserEventType: uint
    {
        None = 0,

        UserTest = 0x1, // for testing, not used otherwise
        TwitchChatMessage = 0x2,
        TwitchChatMessageClear = 0x4,
        TwitchChatUserClear = 0x8,
        SpotifyMusicStateUpdate = 0x10,
        SpotifyMusicTrackChanged = 0x20,
        TwitchChannelPointsRedemption = 0x40,
        TwitchSubscription = 0x80,
        TwitchBitsCheer = 0x100,
    }

    [Flags]
    public enum GlobalEventType: uint
    {
        None = 0,
        GlobalTest = 0x1, // for testing, not used otherwise
    }
}