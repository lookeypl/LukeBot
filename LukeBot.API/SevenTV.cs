using LukeBot.Common;
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

        private class SevenTVEmote: Emote
        {
            public SevenTVEmote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = 0;
                height = 0;

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

        private static void FillEmotes(ResponseJObject resp, ref EmoteSet set)
        {
            foreach (var e in resp.obj["emote_set"]["emotes"])
            {
                set.AddEmote(new SevenTVEmote(e as JObject));
            }
        }

        private static EmoteSet GetEmotes(string URI)
        {
            EmoteSet set = EmoteSet.Empty();

            Logger.Log().Secure("7TV: Requesting URI {0}", URI);

            ResponseJObject resp = Request.GetJObject(URI);

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
            return EmoteSet.Empty();
        }

        public static EmoteSet GetUserEmotes(string userID)
        {
            return GetEmotes(SEVENTV_API_TWITCH_EMOTES_URI + "/" + userID);
        }
    }
}