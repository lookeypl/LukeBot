using LukeBot.Common;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class SevenTV
    {
        private const string SEVENTV_API_BASE_URI = "https://7tv.io/v3/";
        private const string SEVENTV_API_GLOBAL_EMOTES_URI = SEVENTV_API_BASE_URI + "emotes/global";
        private const string SEVENTV_API_EMOTES_URI = SEVENTV_API_BASE_URI + "users/";
        private const string SEVENTV_API_EMOTES_URI_TAIL = "/emotes";

        private class SevenTVEmote: Emote
        {
            public SevenTVEmote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = (int)e["width"][0];
                height = (int)e["height"][0];
            }
        }

        private static void FillEmotes(ResponseJArray resp, ref EmoteSet set)
        {
            foreach (var e in resp.array)
            {
                set.AddEmote(new SevenTVEmote(e as JObject));
            }
        }

        private static EmoteSet GetEmotes(string URI)
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJArray resp = Request.GetJArray(URI);

            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("7TV: Failed to fetch emotes from URI {0} - {1}", URI, resp.code.ToString());
                return EmoteSet.Empty();
            }

            FillEmotes(resp, ref set);
            return set;
        }

        public static EmoteSet GetGlobalEmotes()
        {
            return GetEmotes(SEVENTV_API_GLOBAL_EMOTES_URI);
        }

        public static EmoteSet GetUserEmotes(string user)
        {
            return GetEmotes(SEVENTV_API_EMOTES_URI + user + SEVENTV_API_EMOTES_URI_TAIL);
        }
    }
}