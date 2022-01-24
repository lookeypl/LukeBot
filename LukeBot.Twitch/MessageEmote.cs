using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LukeBot.Twitch
{
    public struct MessageEmoteRange
    {
        public int From { get; private set; }
        public int To { get; private set; }

        public MessageEmoteRange(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    public struct MessageEmote
    {
        public string Source { get; private set; }
        public string Name { get; private set; }
        public string ID { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<MessageEmoteRange> Ranges { get; private set; }

        public MessageEmote(Emote emote, int from, int to)
        {
            Source = emote.Source.ToString();
            Name = emote.Name;
            ID = emote.ID;
            Width = emote.Width;
            Height = emote.Height;
            Ranges = new List<MessageEmoteRange>();
            Ranges.Add(new MessageEmoteRange(from, to));
        }

        public MessageEmote(EmoteSource source, string name, string id, int width, int height, string rangesStr)
        {
            Source = source.ToString();
            Name = name;
            ID = id;
            Width = width;
            Height = height;
            Ranges = new List<MessageEmoteRange>();
            string[] ranges = rangesStr.Split(',');
            foreach (string r in ranges)
            {
                int dash = r.IndexOf('-');
                Ranges.Add(
                    new MessageEmoteRange(
                        Int32.Parse(r.Substring(0, dash)), Int32.Parse(r.Substring(dash + 1))
                    )
                );
            }
        }
    }
}
