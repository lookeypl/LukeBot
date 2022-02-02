using System.Collections.Generic;
using System.Net;
using LukeBot.Common;
using LukeBot.Auth;
using LukeBot.Twitch.Common;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{

    class SevenTVEmoteSource: IEmoteSource
    {
        private const string SEVENTV_API_BASE_URI = "https://api.7tv.app/v2/";
        private const string SEVENTV_API_GLOBAL_EMOTES_URI = SEVENTV_API_BASE_URI + "emotes/global";
        private const string SEVENTV_API_EMOTES_URI = SEVENTV_API_BASE_URI + "users/";
        private const string SEVENTV_API_EMOTES_URI_TAIL = "/emotes";

        private string mUser;

        private struct SevenTVEmote
        {
            public string id { get; set; }
            public string name { get; set; }
            public int[] width { get; set; }
            public int[] height { get; set; }
        };

        public SevenTVEmoteSource(string user)
        {
            mUser = user;
        }

        private void AddEmoteDataToSet(ResponseJArray responseObj, ref Dictionary<string, Emote> emoteSet)
        {
            foreach (var o in responseObj.array)
            {
                string name = (string)o["name"];
                if (emoteSet.ContainsKey(name))
                {
                    Logger.Log().Warning("7TV: Skipping duplicate emote name already found in the set: {0}", name);
                }
                else
                {
                    emoteSet.Add(name, new Emote(EmoteSource.SevenTV, name, (string)o["id"], (int)o["width"][0], (int)o["height"][0]));
                }
            }
        }

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
            // global emotes
            ResponseJArray globalData = Request.GetJArray(SEVENTV_API_GLOBAL_EMOTES_URI, null, null);
            if (globalData.code != HttpStatusCode.OK)
            {
                throw new APIErrorException("Failed to fetch 7TV global emote data: " + globalData.code.ToString());
            }

            // user emotes
            ResponseJArray userData = Request.GetJArray(SEVENTV_API_EMOTES_URI + mUser + SEVENTV_API_EMOTES_URI_TAIL, null, null);
            if (userData.code != HttpStatusCode.OK)
            {
                throw new APIErrorException("Failed to fetch 7TV user data: " + userData.code.ToString());
            }

            AddEmoteDataToSet(userData, ref emoteSet);
            AddEmoteDataToSet(globalData, ref emoteSet);
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
