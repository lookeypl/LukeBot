using System;
using System.IO;
using System.Threading;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Events;
using LukeBot.Spotify.Common;


namespace LukeBot.Widget
{
    public class NowPlaying: IWidget
    {
        SpotifyMusicStateUpdateArgs mState;
        SpotifyMusicTrackChangedArgs mCurrentTrack;

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyMusicStateUpdateArgs a = (SpotifyMusicStateUpdateArgs)args;
            mState = a;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyMusicTrackChangedArgs a = (SpotifyMusicTrackChangedArgs)args;
            mCurrentTrack = a;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        private void OnConnected(object o, EventArgs e)
        {
            if (mState != null && mState.State != PlayerState.Unloaded)
            {
                // Push a state update to "pre-refresh" the widget
                OnTrackChanged(null, mCurrentTrack);
                OnStateUpdate(null, mState);
            }
        }

        public NowPlaying()
            : base("LukeBot.Widget/Widgets/NowPlaying.html")
        {
            mState = null;
            mCurrentTrack = null;

            Comms.Event.SpotifyMusicStateUpdate += OnStateUpdate;
            Comms.Event.SpotifyMusicTrackChanged += OnTrackChanged;

            OnConnectedEvent += OnConnected;
        }

        ~NowPlaying()
        {
        }
    }
}
