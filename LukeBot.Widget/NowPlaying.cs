using System;
using System.IO;
using System.Threading;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Spotify.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class NowPlaying: IWidget
    {
        SpotifyStateUpdateArgs mState;
        SpotifyTrackChangedArgs mCurrentTrack;

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyStateUpdateArgs a = (SpotifyStateUpdateArgs)args;
            mState = a;
            SendToWSAsync(JsonSerializer.Serialize(a));
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyTrackChangedArgs a = (SpotifyTrackChangedArgs)args;
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

        public NowPlaying(string lbUser, string id, string name)
            : base("LukeBot.Widget/Widgets/NowPlaying.html", id, name)
        {
            mState = null;
            mCurrentTrack = null;

            Comms.Event.User(lbUser).Event(Events.SPOTIFY_STATE_UPDATE).Endpoint += OnStateUpdate;
            Comms.Event.User(lbUser).Event(Events.SPOTIFY_TRACK_CHANGED).Endpoint += OnTrackChanged;

            OnConnectedEvent += OnConnected;
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.nowplaying;
        }

        ~NowPlaying()
        {
        }
    }
}
