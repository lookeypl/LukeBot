using System.Collections.Generic;
using LukeBot.Twitch;


namespace LukeBot.Twitch.Impl
{
    public interface IEmoteSource
    {
        void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet);
        void GetEmoteInfo();
    };
}
