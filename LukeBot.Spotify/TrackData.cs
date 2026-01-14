namespace LukeBot.Spotify
{
    public struct TrackData
    {
        public string Artist;
        public string Title;

        public TrackData(string artist, string title)
        {
            Artist = artist;
            Title = title;
        }
    }
}
