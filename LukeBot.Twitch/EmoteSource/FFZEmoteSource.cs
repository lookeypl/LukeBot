using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{

    class FFZEmoteSource: IEmoteSource
    {
        private string mUserID;


        public FFZEmoteSource(string twitchID)
        {
            mUserID = twitchID;
        }

        public void FetchEmoteSet(ref Dictionary<string, Twitch.Common.Emote> emoteSet)
        {
            API.EmoteSet globalEmotes = API.FFZ.GetGlobalEmotes();
            API.EmoteSet userEmotes = API.FFZ.GetUserEmotes(mUserID);

            foreach (var e in globalEmotes.emotes)
            {
                emoteSet.Add(e.name, new Twitch.Common.Emote(EmoteSource.FFZ, e.name, e.id, e.width, e.height));
            }

            foreach (var e in userEmotes.emotes)
            {
                emoteSet.Add(e.name, new Twitch.Common.Emote(EmoteSource.FFZ, e.name, e.id, e.width, e.height));
            }
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
