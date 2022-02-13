using System.Collections.Generic;
using System.Net;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Twitch.Common;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{

    class SevenTVEmoteSource: IEmoteSource
    {
        private string mUser;

        public SevenTVEmoteSource(string user)
        {
            mUser = user;
        }

        private void AddEmoteDataToSet(API.SevenTV.EmoteSet fetchedSet, ref Dictionary<string, Emote> emoteSet)
        {
            foreach (var o in fetchedSet.emotes)
            {
                if (emoteSet.ContainsKey(o.name))
                {
                    Logger.Log().Warning("7TV: Skipping duplicate emote name already found in the set: {0}", o.name);
                }
                else
                {
                    emoteSet.Add(o.name, new Emote(EmoteSource.SevenTV, o.name, o.id, o.width, o.height));
                }
            }
        }

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
            API.SevenTV.EmoteSet globalSet = API.SevenTV.GetGlobalEmotes();
            API.SevenTV.EmoteSet userSet = API.SevenTV.GetUserEmotes(mUser);

            AddEmoteDataToSet(globalSet, ref emoteSet);
            AddEmoteDataToSet(userSet, ref emoteSet);
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
