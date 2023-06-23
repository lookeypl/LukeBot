using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LukeBot.API
{
    public class Emote
    {
        public string id { get; protected set; }
        public string name { get; protected set; }
        public int width { get; protected set; }
        public int height { get; protected set; }

        public Emote()
            : this("", "", 0, 0)
        {
        }

        public Emote(string id, string name, int width, int height)
        {
            this.id = id;
            this.name = name;
            this.width = width;
            this.height = height;
        }
    };

    public class EmoteSet
    {
        public List<Emote> emotes { get; private set; }

        public EmoteSet()
        {
            emotes = new List<Emote>();
        }

        public void AddEmote(Emote e)
        {
            emotes.Add(e);
        }

        public static EmoteSet Empty()
        {
            return new EmoteSet();
        }
    };
}