using System.Collections.Generic;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class SevenTV
    {
        private const string SEVENTV_API_BASE_URI = "https://api.7tv.app/v2/";
        private const string SEVENTV_API_GLOBAL_EMOTES_URI = SEVENTV_API_BASE_URI + "emotes/global";
        private const string SEVENTV_API_EMOTES_URI = SEVENTV_API_BASE_URI + "users/";
        private const string SEVENTV_API_EMOTES_URI_TAIL = "/emotes";

        public struct Emote
        {
            public string id { get; private set; }
            public string name { get; private set; }
            public int width { get; private set; }
            public int height { get; private set; }

            public Emote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = (int)e["width"][0];
                height = (int)e["height"][0];
            }
        };

        public class EmoteSet
        {
            public List<Emote> emotes { get; private set; }

            public EmoteSet(ResponseJArray data)
            {
                emotes = new List<Emote>();

                foreach (var e in data.array)
                {
                    emotes.Add(new Emote((JObject)e));
                }
            }
        };

        public static EmoteSet GetGlobalEmotes()
        {
            return new EmoteSet(Request.GetJArray(SEVENTV_API_GLOBAL_EMOTES_URI));
        }

        public static EmoteSet GetUserEmotes(string user)
        {
            return new EmoteSet(Request.GetJArray(SEVENTV_API_EMOTES_URI + user + SEVENTV_API_EMOTES_URI_TAIL));
        }
    }
}