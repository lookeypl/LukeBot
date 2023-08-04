using System;


namespace LukeBot.Communication.Common
{
    public class SpotifyMusicTrackChangedArgs: UserEventArgsBase
    {
        public string Artists { get; private set; }
        public string Title { get; private set; }
        public string Label { get; private set; }
        public float Duration { get; private set; }

        public SpotifyMusicTrackChangedArgs(string artists, string title, string label, float duration)
            : base(UserEventType.SpotifyMusicTrackChanged, "NowPlayingTrackChanged")
        {
            Artists = artists;
            Title = title;
            Label = label;
            Duration = duration;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1} ({2})", Artists, Title, Duration);
        }
    };
}
