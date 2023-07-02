using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{

    class SevenTVEmoteSource: IEmoteSource
    {
        private string mUserID;

        public SevenTVEmoteSource(string twitchID)
        {
            mUserID = twitchID;
        }

        private void AddEmoteDataToSet(API.EmoteSet fetchedSet, ref Dictionary<string, Twitch.Common.Emote> emoteSet)
        {
            foreach (var o in fetchedSet.emotes)
            {
                if (emoteSet.ContainsKey(o.name))
                {
                    Logger.Log().Warning("7TV: Skipping duplicate emote name already found in the set: {0}", o.name);
                }
                else
                {
                    emoteSet.Add(o.name, new Twitch.Common.Emote(EmoteSource.SevenTV, o.name, o.id, o.width, o.height));
                }
            }
        }

        public void FetchEmoteSet(ref Dictionary<string, Twitch.Common.Emote> emoteSet)
        {
            API.EmoteSet globalSet = API.SevenTV.GetGlobalEmotes();
            API.EmoteSet userSet = API.SevenTV.GetUserEmotes(mUserID);

            AddEmoteDataToSet(globalSet, ref emoteSet);
            AddEmoteDataToSet(userSet, ref emoteSet);
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }
}
