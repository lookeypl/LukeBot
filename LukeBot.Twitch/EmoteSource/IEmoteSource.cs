using System.Collections.Generic;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{
    public interface IEmoteSource
    {
        void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet);
        void GetEmoteInfo();
    };
}
