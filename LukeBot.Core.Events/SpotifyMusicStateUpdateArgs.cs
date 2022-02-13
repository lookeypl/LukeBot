using System;
using LukeBot.Spotify.Common;


namespace LukeBot.Core.Events
{
    public class SpotifyMusicStateUpdateArgs: EventArgsBase
    {
        private const string NOW_PLAYING_STATE_UPDATE_TYPE_STR = "NowPlayingStateUpdate";

        public PlayerState State { get; private set; }
        public float Progress { get; private set; }

        public SpotifyMusicStateUpdateArgs()
            : base(Events.Type.SpotifyMusicStateUpdate, NOW_PLAYING_STATE_UPDATE_TYPE_STR)
        {
            State = PlayerState.Unloaded;
            Progress = 0.0f;
        }

        public SpotifyMusicStateUpdateArgs(PlayerState state, float progress)
            : base(Events.Type.SpotifyMusicStateUpdate, NOW_PLAYING_STATE_UPDATE_TYPE_STR)
        {
            State = state;
            Progress = progress;
        }

        public static bool operator ==(SpotifyMusicStateUpdateArgs a, SpotifyMusicStateUpdateArgs b)
        {
            return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b);
        }

        public static bool operator !=(SpotifyMusicStateUpdateArgs a, SpotifyMusicStateUpdateArgs b)
        {
            return !(a == b);
        }

        public override bool Equals(Object o)
        {
            SpotifyMusicStateUpdateArgs other = (SpotifyMusicStateUpdateArgs)o;
            return !object.ReferenceEquals(o, null) &&
                (State == other.State && Progress == other.Progress);
        }

        public override int GetHashCode()
        {
            return State.GetHashCode() ^ Progress.GetHashCode();
        }
    }
}
