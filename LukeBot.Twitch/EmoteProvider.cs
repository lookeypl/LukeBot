using System.Collections.Generic;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{
    public class EmoteProvider
    {
        private List<IEmoteSource> mEmoteSources;
        private Dictionary<string, Emote> mEmoteSet;


        public EmoteProvider()
        {
            mEmoteSources = new List<IEmoteSource>();
            mEmoteSet = new Dictionary<string, Emote>();
        }

        public void AddEmoteSource(IEmoteSource source)
        {
            mEmoteSources.Add(source);
            source.FetchEmoteSet(ref mEmoteSet);
        }

        public List<MessageEmote> ParseEmotes(string str)
        {
            List<MessageEmote> result = new List<MessageEmote>();

            string[] tokens = str.Split(' ', System.StringSplitOptions.None);
            int from = 0;
            Emote emote;
            foreach (string t in tokens)
            {
                if (mEmoteSet.TryGetValue(t, out emote))
                {
                    result.Add(new MessageEmote(emote, from, from + t.Length - 1));
                }

                from += t.Length + 1; // length of token + space
            }

            return result;
        }

        public void Refresh()
        {
            mEmoteSet.Clear();

            foreach (IEmoteSource source in mEmoteSources)
            {
                source.FetchEmoteSet(ref mEmoteSet);
            }
        }
    }

}
