namespace LukeBot.Twitch.Common
{
    public class Emote
    {
        public EmoteSource Source { get; private set; }
        public string Name { get; private set; }
        public string ID { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Emote(EmoteSource source, string name, string id, int width, int height)
        {
            Source = source;
            Name = name;
            ID = id;
            Width = width;
            Height = height;
        }
    }
}
