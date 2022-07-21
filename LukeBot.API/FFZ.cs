using System.Collections.Generic;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class FFZ
    {
        private const string FFZ_API_BASE_URI = "https://api.frankerfacez.com/v1/";
        private const string FFZ_API_GLOBAL_SET_URI  = FFZ_API_BASE_URI + "set/global";
        private const string FFZ_API_ROOM_URI = FFZ_API_BASE_URI + "room/id/";

        public struct Emote
        {
            // name, id, width, height, ...?
            public string name { get; private set; }
            public string id { get; private set; }
            public int width { get; private set; }
            public int height { get; private set; }

            public Emote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = (int)e["width"];
                height = (int)e["height"];
            }
        }

        public class EmoteSet
        {
            public List<Emote> emotes { get; private set; }

            public EmoteSet()
            {
                emotes = new List<Emote>();
            }

            private void AddEmotes(JArray set)
            {
                foreach (var e in set)
                {
                    emotes.Add(new Emote(e as JObject));
                }
            }

            public static EmoteSet FromGlobalEmotes(ResponseJObject data)
            {
                EmoteSet set = new EmoteSet();

                JArray defaultSets = data.obj["default_sets"] as JArray;
                foreach (string globalSetID in defaultSets)
                {
                    set.AddEmotes(data.obj["sets"][globalSetID]["emoticons"] as JArray);
                }

                return set;
            }

            public static EmoteSet FromUserEmotes(ResponseJObject data)
            {
                EmoteSet set = new EmoteSet();

                string setID = (string)data.obj["room"]["set"];
                set.AddEmotes(data.obj["sets"][setID]["emoticons"] as JArray);

                return set;
            }
        }


        public static EmoteSet GetGlobalEmotes()
        {
            return EmoteSet.FromGlobalEmotes(Request.GetJObject(FFZ_API_GLOBAL_SET_URI));
        }

        public static EmoteSet GetUserEmotes(string userID)
        {
            return EmoteSet.FromUserEmotes(Request.GetJObject(FFZ_API_ROOM_URI + userID));
        }
    }
}