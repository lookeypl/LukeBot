using System.Collections.Generic;
using System.Net;
using LukeBot.Common;
using LukeBot.Auth;
using Newtonsoft.Json;


namespace LukeBot.Twitch
{

    class SevenTVEmoteSource: IEmoteSource
    {
        private const string SEVENTV_API_BASE_URI = "https://api.7tv.app/";
        private const string SEVENTV_API_VERSION = "v2";
        private const string SEVENTV_API_EMOTES_URI = SEVENTV_API_BASE_URI + SEVENTV_API_VERSION + "/users/";
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

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
            ResponseJArray userData = Request.GetJArray(SEVENTV_API_EMOTES_URI + mUser + SEVENTV_API_EMOTES_URI_TAIL, null, null);
            if (userData.code != HttpStatusCode.OK)
            {
                throw new APIErrorException("Failed to fetch 7TV user data: " + userData.code.ToString());
            }

            foreach (var o in userData.array)
            {
                string name = (string)o["name"];
                emoteSet.Add(name, new Emote(EmoteSource.SevenTV, name, (string)o["id"], (int)o["width"][0], (int)o["height"][0]));
            }
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
