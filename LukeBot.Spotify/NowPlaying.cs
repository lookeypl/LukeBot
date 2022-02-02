using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Auth;
using LukeBot.Spotify.Common;
using LukeBot.Core;
using LukeBot.Core.Events;


namespace LukeBot.Spotify
{
    class NowPlaying: IEventPublisher
    {
        private readonly int DEFAULT_EVENT_TIMEOUT = 5 * 1000; // 5 seconds
        private readonly int EXTRA_EVENT_TIMEOUT = 2000; // see FetchData() for details
        private readonly string REQUEST_URI = "https://api.spotify.com/v1/me/player";

        private Token mToken;
        private Thread mThread;
        private Mutex mDataAccessMutex;
        private ManualResetEvent mShutdownEvent;
        private Data mCurrentData;
        private SpotifyMusicStateUpdateArgs mCurrentState;
        private int mEventTimeout;
        private bool mChangeExpected;
        private EventCallback mTrackChangedCallback;
        private EventCallback mStateUpdateCallback;

        public NowPlaying(Token token)
        {
            mToken = token;
            mThread = new Thread(new ThreadStart(ThreadMain));
            mDataAccessMutex = new Mutex();
            mShutdownEvent = new ManualResetEvent(false);
            mEventTimeout = DEFAULT_EVENT_TIMEOUT;
            mChangeExpected = false;
            mCurrentState = new SpotifyMusicStateUpdateArgs();

            List<EventCallback> events = Systems.Event.RegisterEventPublisher(
                this, Core.Events.Type.SpotifyMusicStateUpdate | Core.Events.Type.SpotifyMusicTrackChanged
            );

            foreach (EventCallback e in events)
            {
                switch (e.type)
                {
                case Core.Events.Type.SpotifyMusicStateUpdate:
                    mStateUpdateCallback = e;
                    break;
                case Core.Events.Type.SpotifyMusicTrackChanged:
                    mTrackChangedCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }
        }

        ~NowPlaying()
        {
        }

        void FetchData()
        {
            Data data = Request.Get<Data>(REQUEST_URI, mToken, null);
            if (data.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Debug("OAuth token expired - refreshing...");
                mToken.Refresh();
                data = Request.Get<Data>(REQUEST_URI, mToken, null);
            }

            if (data.code != HttpStatusCode.OK)
            {
                if (data.code != HttpStatusCode.NoContent)
                    Logger.Log().Error("Failed to fetch Now Playing data: {0}", data.code);

                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;

                return;
            }

            if (data.item == null)
            {
                Logger.Log().Warning("No track item received");
                return;
            }

            mDataAccessMutex.WaitOne();

            // Track change
            if ((mCurrentData == null) || (data.item.id != mCurrentData.item.id))
            {
                // Spotify doesn't provide copyright holder info (aka label info) with
                // currently played track API call. For that reason we will fetch the info
                // separately from album details and copy it to our "data" object for
                // further reference. Shallow copy should be ok.
                DataDetailedAlbum album = Request.Get<DataDetailedAlbum>(data.item.album.href, mToken, null);
                data.item.album.copyrights = album.copyrights;

                mCurrentData = data;
                mChangeExpected = false;
                mTrackChangedCallback.PublishEvent(Utils.DataItemToTrackChangedArgs(mCurrentData.item));
            }

            // State read - mCurrentData is valid at this point
            SpotifyMusicStateUpdateArgs state = Utils.DataToStateUpdateArgs(data);

            // Update internal logic according to state
            if (state.State == Common.PlaybackState.Playing)
            {
                if (mChangeExpected)
                {
                    // if mChangeExpected is set here, that means server must've lagged a bit
                    // and new song is still not updated. Rush the next update just in case.
                    mEventTimeout = 1000;
                }
                else
                {
                    int trackLeftMs = mCurrentData.item.duration_ms - mCurrentData.progress_ms;
                    if (trackLeftMs < DEFAULT_EVENT_TIMEOUT)
                    {
                        // We are close to switch to new track.
                        // To make the switch more "instantenous" we could wait only for as long
                        // as it takes for our track to go to finish.
                        // Add extra timeout to let server fetch new data
                        mEventTimeout = trackLeftMs + EXTRA_EVENT_TIMEOUT;
                        mChangeExpected = true;
                    }
                    else
                    {
                        // Default timeout, track is still somewhere in the middle
                        mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                        mChangeExpected = false;
                    }
                }
            }
            else
            {
                // Track unloaded or stopped; timeout default
                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;
            }

            if (state != mCurrentState)
            {
                mCurrentState = state;
                mStateUpdateCallback.PublishEvent(state);
            }

            mDataAccessMutex.ReleaseMutex();
        }

        void ThreadMain()
        {
            // mShutdownEvent will serve as our "timer"
            while (true)
            {
                if (mShutdownEvent.WaitOne(mEventTimeout))
                {
                    Logger.Log().Debug("Shutdown event triggered - closing");
                    break;
                }

                // no signal means no shutdown requested - continue as normal
                try
                {
                    FetchData();
                }
                catch (System.Exception e)
                {
                    Logger.Log().Warning("Caught exception while fetching data: {0}. Ignoring and continuing anyway...", e.Message);
                }
            }
        }

        public void Run()
        {
            mThread.Start();
        }

        public void RequestShutdown()
        {
            mShutdownEvent.Set();
        }

        public void Wait()
        {
            mThread.Join();
        }

        public Data GetData()
        {
            mDataAccessMutex.WaitOne();
            Data data = mCurrentData;
            mDataAccessMutex.ReleaseMutex();
            return data;
        }
    }
}
