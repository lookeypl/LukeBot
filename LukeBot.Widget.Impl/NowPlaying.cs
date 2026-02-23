using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Services;
using LukeBot.Spotify;


namespace LukeBot.Widget.Impl
{
    /**
     * Widget reflecting currently played track on Spotify service.
     *
     * Reacts to events:
     *  - SpotifyStateUpdate
     *  - SpotifyTrackChanged
     */
    public class NowPlaying: IWidget
    {
        SpotifyStateUpdateArgs mState;
        SpotifyTrackChangedArgs mCurrentTrack;

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyStateUpdateArgs a = (SpotifyStateUpdateArgs)args;
            mState = a;
            SendToWS(a);
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyTrackChangedArgs a = (SpotifyTrackChangedArgs)args;
            mCurrentTrack = a;
            SendToWS(a);
        }

        protected override void OnConnected()
        {
            if (mState != null && mState.State != PlayerState.Unloaded)
            {
                // Push a state update to "pre-refresh" the widget
                OnTrackChanged(null, mCurrentTrack);
                OnStateUpdate(null, mState);
            }
        }

        protected override void OnDisconnected()
        {
        }

        protected override void OnLoad()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.SPOTIFY_STATE_UPDATE).Subscribe(OnStateUpdate);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.SPOTIFY_TRACK_CHANGED).Subscribe(OnTrackChanged);
        }

        protected override void OnUnload()
        {
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.SPOTIFY_STATE_UPDATE).Unsubscribe(OnStateUpdate);
            ServiceUtils.GetEventService().User(mLBUser).Event(Events.SPOTIFY_TRACK_CHANGED).Unsubscribe(OnTrackChanged);
        }

        protected override ConfigurationBase CreateDefaultConfiguration()
        {
            return new EmptyWidgetConfiguration();
        }

        public NowPlaying(string lbUser, string id, string name)
            : base(lbUser, "Widgets/NowPlaying.html", id, name)
        {
            mState = null;
            mCurrentTrack = null;
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
