using System.Collections.Generic;
using System.Net;
using LukeBot.Common;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class BTTV
    {
        private const string BTTV_API_BASE_URI = "https://api.betterttv.net/3/cached";
        private const string BTTV_API_GLOBAL_SET_URI = BTTV_API_BASE_URI + "/emotes/global";
        private const string BTTV_API_USER_URI = BTTV_API_BASE_URI + "/users/twitch";

        private class BTTVEmote: Emote
        {
            public BTTVEmote(JObject e)
            {
                id = (string)e["id"];
                name = (string)e["code"];
                // TODO happy assumption, but BTTV does not provide this.
                // Make sure it's needed, and either try and fetch the emote here or remove
                // width/height completely.
                width = 128;
                height = 128;
            }
        }

        private static void AddEmotes(JArray recvSet, ref EmoteSet set)
        {
            foreach (var e in recvSet)
            {
                set.AddEmote(new BTTVEmote(e as JObject));
            }
        }

        private static void FillSetFromUserEmotes(ResponseJObject data, ref EmoteSet set)
        {
            AddEmotes(data.obj["channelEmotes"] as JArray, ref set);
            AddEmotes(data.obj["sharedEmotes"] as JArray, ref set);
        }

        public static EmoteSet GetGlobalEmotes()
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJArray resp = Request.GetJArray(BTTV_API_GLOBAL_SET_URI);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("BTTV: Failed to fetch Global emotes - {0}", resp.code.ToString());
                return set;
            }

            AddEmotes(resp.array, ref set);
            return set;
        }

        public static EmoteSet GetUserEmotes(string userID)
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJObject resp = Request.GetJObject(BTTV_API_USER_URI + "/" + userID);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("BTTV: Failed to fetch User {0} emotes - {1}", userID, resp.code.ToString());
                return EmoteSet.Empty();
            }

            FillSetFromUserEmotes(resp, ref set);
            return set;
        }
    }
}