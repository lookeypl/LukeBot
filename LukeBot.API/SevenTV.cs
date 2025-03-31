using LukeBot.Logging;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class SevenTV
    {
        private const string SEVENTV_API_BASE_URI = "https://7tv.io/v3";
        private const string SEVENTV_API_USERS_URI = SEVENTV_API_BASE_URI + "/users";
        private const string SEVENTV_API_TWITCH_EMOTES_URI = SEVENTV_API_USERS_URI + "/twitch";
        private const string SEVENTV_API_EMOTE_SETS_GLOBAL = SEVENTV_API_BASE_URI + "/emote-sets/global";

        private class SevenTVEmote: Emote
        {
            public SevenTVEmote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = 0;
                height = 0;
                animated = false; // 7TV does not differentiate between static and animated emotes
                                  // if an emote is animated it is animated and there is no static version

                JArray files = (JArray)e["data"]["host"]["files"];
                foreach (JObject o in files)
                {
                    if (((string)o["format"]).Equals("WEBP"))
                    {
                        int w = (int)o["width"];
                        int h = (int)o["height"];
                        if (w > width && h > height)
                        {
                            width = w;
                            height = h;
                        }
                    }
                }
            }
        }

        private static void FillEmotes(JToken emoteSetToken, ref EmoteSet set)
        {
            foreach (var e in emoteSetToken["emotes"])
            {
                set.AddEmote(new SevenTVEmote(e as JObject));
            }
        }

        private static EmoteSet GetEmotes(string URI)
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJObject resp = Request.GetJObject(URI);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("7TV: Failed to fetch emotes from URI {0} - {1}", URI, resp.code.ToString());
                return EmoteSet.Empty();
            }

            JToken emoteSet = resp.obj["emote_set"];
            if (emoteSet == null)
            {
                Logger.Log().Warning("7TV: Received empty emote set");
                return EmoteSet.Empty();
            }

            FillEmotes(emoteSet, ref set);
            return set;
        }

        public static EmoteSet GetGlobalEmotes()
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJObject resp = Request.GetJObject(SEVENTV_API_EMOTE_SETS_GLOBAL);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("7TV: Failed to fetch global emotes from 7TV - {0}", resp.code.ToString());
                return EmoteSet.Empty();
            }

            JToken emoteSet = resp.obj;
            if (emoteSet == null)
            {
                Logger.Log().Warning("7TV: Received empty global emote set");
                return EmoteSet.Empty();
            }

            FillEmotes(emoteSet, ref set);
            return set;
        }

        public static EmoteSet GetUserEmotes(string userID)
        {
            return GetEmotes(SEVENTV_API_TWITCH_EMOTES_URI + "/" + userID);
        }
    }
}