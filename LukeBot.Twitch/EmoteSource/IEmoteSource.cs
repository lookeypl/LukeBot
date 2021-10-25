using System.Collections.Generic;


namespace LukeBot.Twitch
{

    public interface IEmoteSource
    {
        void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet);
        void GetEmoteInfo();
    };

}
