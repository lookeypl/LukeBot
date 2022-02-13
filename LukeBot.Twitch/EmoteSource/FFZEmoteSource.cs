using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{

    class FFZEmoteSource: IEmoteSource
    {
        private const string FFZ_API_BASE_URI = "https://api.frankerfacez.com/v1/";
        private const string FFZ_API_GLOBAL_SET_URI  = FFZ_API_BASE_URI + "set/global";
        private const string FFZ_API_ROOM_URI = FFZ_API_BASE_URI + "room/id/";

        private string mUserID;


        public FFZEmoteSource(string twitchID)
        {
            mUserID = twitchID;
        }

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
            // global emotes
            ResponseJObject globalData = Request.GetJObject(FFZ_API_GLOBAL_SET_URI, null, null);
            if (globalData.code != HttpStatusCode.OK)
            {
                throw new APIErrorException("Failed to fetch FFZ global data: " + globalData.code.ToString());
            }

            JArray defaultSets = (JArray)globalData.obj["default_sets"];
            foreach (string globalSetID in defaultSets)
            {
                JArray emotes = (JArray)globalData.obj["sets"][globalSetID]["emoticons"];
                foreach (var e in emotes)
                {
                    string name = (string)e["name"];
                    if (emoteSet.ContainsKey(name))
                    {
                        Logger.Log().Warning("FFZ: Skipping duplicate emote name already found in the set: {0}", name);
                    }
                    else
                    {
                        emoteSet.Add(name, new Emote(EmoteSource.FFZ, name, (string)e["id"], (int)e["width"], (int)e["height"]));
                    }
                }
            }

            // user emotes
            ResponseJObject userData = Request.GetJObject(FFZ_API_ROOM_URI + mUserID, null, null);
            if (userData.code != HttpStatusCode.OK)
            {
                throw new APIErrorException("Failed to fetch FFZ user data: " + userData.code.ToString());
            }

            string setID = (string)userData.obj["room"]["set"];
            JArray emoticons = (JArray)userData.obj["sets"][setID]["emoticons"];
            foreach (var e in emoticons)
            {
                string name = (string)e["name"];
                emoteSet.Add(name, new Emote(EmoteSource.FFZ, name, (string)e["id"], (int)e["width"], (int)e["height"]));
            }
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
