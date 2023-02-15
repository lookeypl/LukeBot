using System;


namespace LukeBot.Communication.Events
{
    [Flags]
    public enum Type
    {
        None = 0,
        TwitchChatMessage = 0x1,
        TwitchChatMessageClear = 0x2,
        TwitchChatUserClear = 0x4,
        SpotifyMusicStateUpdate = 0x8,
        SpotifyMusicTrackChanged = 0x10,
        TwitchChannelPointsRedemption = 0x20,
        TwitchSubscription = 0x40,
        TwitchBitsCheer = 0x80,
    }
}