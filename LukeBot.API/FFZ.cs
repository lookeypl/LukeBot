using System.Collections.Generic;
using System.Net;
using LukeBot.Common;
using Newtonsoft.Json.Linq;


namespace LukeBot.API
{
    public class FFZ
    {
        private const string FFZ_API_BASE_URI = "https://api.frankerfacez.com/v1/";
        private const string FFZ_API_GLOBAL_SET_URI  = FFZ_API_BASE_URI + "set/global";
        private const string FFZ_API_ROOM_URI = FFZ_API_BASE_URI + "room/id/";

        private class FFZEmote: Emote
        {
            public FFZEmote(JObject e)
            {
                name = (string)e["name"];
                id = (string)e["id"];
                width = (int)e["width"];
                height = (int)e["height"];
            }
        }

        private static void AddEmotes(JArray recvSet, ref EmoteSet set)
        {
            foreach (var e in recvSet)
            {
                set.AddEmote(new FFZEmote(e as JObject));
            }
        }

        private static void FillSetFromGlobalEmotes(ResponseJObject data, ref EmoteSet set)
        {
            JArray defaultSets = data.obj["default_sets"] as JArray;
            foreach (string globalSetID in defaultSets)
            {
                AddEmotes(data.obj["sets"][globalSetID]["emoticons"] as JArray, ref set);
            }
        }

        private static void FillSetFromUserEmotes(ResponseJObject data, ref EmoteSet set)
        {
            string setID = (string)data.obj["room"]["set"];
            AddEmotes(data.obj["sets"][setID]["emoticons"] as JArray, ref set);
        }

        public static EmoteSet GetGlobalEmotes()
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJObject resp = Request.GetJObject(FFZ_API_GLOBAL_SET_URI);

            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("FFZ: Failed to fetch Global emotes - {0}", resp.code.ToString());
                return set;
            }

            FillSetFromGlobalEmotes(resp, ref set);
            return set;
        }

        public static EmoteSet GetUserEmotes(string userID)
        {
            EmoteSet set = EmoteSet.Empty();

            ResponseJObject resp = Request.GetJObject(FFZ_API_ROOM_URI + userID);

            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Warning("FFZ: Failed to fetch User {0} emotes - {1}", userID, resp.code.ToString());
                return EmoteSet.Empty();
            }

            FillSetFromUserEmotes(resp, ref set);
            return set;
        }
    }
}