using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Twitch
{

    class FFZEmoteSource: IEmoteSource
    {
        private const string FFZ_API_BASE_URI = "https://api.frankerfacez.com/";
        private const string FFZ_API_VERSION = "v1";
        private const string FFZ_API_ROOM_URI = FFZ_API_BASE_URI + FFZ_API_VERSION + "/room/id/";

        private string mUserID;


        public FFZEmoteSource(string twitchID)
        {
            mUserID = twitchID;
        }

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
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
