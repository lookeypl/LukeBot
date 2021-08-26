using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Spotify
{
    class NowPlaying
    {
        public enum State
        {
            Unloaded = 0,
            Stopped,
            Playing,
        };

        public class DataArtists
        {
            public string name { get; set; }
        };

        public class DataItem
        {
            public List<DataArtists> artists { get; set; }
            public int duration_ms { get; set; }
            public string id { get; set; }
            public string name { get; set; }
        };

        public class Data: Response
        {
            public DataItem item { get; set; }
            public bool is_playing { get; set; }
            public int progress_ms { get; set; }

            public override string ToString()
            {
                string artists = item.artists[0].name;
                for (int i = 1; i < item.artists.Count; ++i)
                {
                    artists += ", ";
                    artists += item.artists[i].name;
                }
                return String.Format("{0} - {1} ({2}/{3})", artists, item.name,
                                     progress_ms / 1000.0f, item.duration_ms / 1000.0f);
            }
        };

        public class TrackChangedArgs
        {
            public string Type { get; private set; }
            public string Artists { get; private set; }
            public string Title { get; private set; }
            public float Duration { get; private set; }

            public static TrackChangedArgs FromDataItem(DataItem item)
            {
                TrackChangedArgs ret = new TrackChangedArgs();
                ret.Type = "NowPlayingTrackChanged"; // to recognize it widget-side
                ret.Duration = item.duration_ms / 1000.0f; // convert to seconds
                ret.Title = item.name;
                ret.Artists = item.artists[0].name;
                for (int i = 1; i < item.artists.Count; ++i)
                {
                    ret.Artists += ", ";
                    ret.Artists += item.artists[i].name;
                }
                return ret;
            }

            public override string ToString()
            {
                return String.Format("{0} - {1} ({2})", Artists, Title, Duration);
            }
        };

        public class StateUpdateArgs
        {
            public string Type { get; private set; }
            public State State { get; private set; }
            public float Progress { get; private set; }

            public static bool operator ==(StateUpdateArgs a, StateUpdateArgs b)
            {
                return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b);
            }

            public static bool operator !=(StateUpdateArgs a, StateUpdateArgs b)
            {
                return !(a == b);
            }

            public override bool Equals(Object o)
            {
                StateUpdateArgs other = (StateUpdateArgs)o;
                return !object.ReferenceEquals(o, null) &&
                    (State == other.State && Progress == other.Progress);
            }

            public override int GetHashCode()
            {
                return State.GetHashCode() ^ Progress.GetHashCode();
            }

            public static StateUpdateArgs FromData(Data data)
            {
                StateUpdateArgs ret = new StateUpdateArgs();
                ret.Type = "NowPlayingStateUpdate"; // to recognize it widget-side
                if (data.code == HttpStatusCode.NoContent)
                {
                    ret.Progress = 0.0f;
                    ret.State = State.Unloaded;
                }
                else
                {
                    ret.Progress = data.progress_ms / 1000.0f; // conversion from ms to s
                    ret.State = data.is_playing ? State.Playing : State.Stopped;
                }
                return ret;
            }

            public StateUpdateArgs()
            {
                State = State.Unloaded;
                Progress = 0.0f;
            }
        }

        private readonly int DEFAULT_EVENT_TIMEOUT = 5 * 1000; // 5 seconds
        private readonly int EXTRA_EVENT_TIMEOUT = 2000; // see FetchData() for details
        private readonly string REQUEST_URI = "https://api.spotify.com/v1/me/player";

        private Token mToken;
        private Thread mThread;
        private Mutex mDataAccessMutex;
        private ManualResetEvent mShutdownEvent;
        private Data mCurrentData;
        private StateUpdateArgs mCurrentState;
        private int mEventTimeout;
        private bool mChangeExpected;

        public EventHandler<TrackChangedArgs> TrackChanged;
        public EventHandler<StateUpdateArgs> StateUpdate;

        public NowPlaying(Token token)
        {
            mToken = token;
            mThread = new Thread(new ThreadStart(ThreadMain));
            mDataAccessMutex = new Mutex();
            mShutdownEvent = new ManualResetEvent(false);
            mEventTimeout = DEFAULT_EVENT_TIMEOUT;
            mChangeExpected = false;
            mCurrentState = new StateUpdateArgs();
        }

        ~NowPlaying()
        {
        }

        private void OnTrackChanged(TrackChangedArgs args)
        {
            EventHandler<TrackChangedArgs> handler = TrackChanged;
            if (handler != null)
                handler(this, args);
        }

        private void OnStateUpdate(StateUpdateArgs args)
        {
            EventHandler<StateUpdateArgs> handler = StateUpdate;
            if (handler != null)
                handler(this, args);
        }

        void FetchData()
        {
            Data data = Request.Get<Data>(REQUEST_URI, mToken, null);
            if (data.code == HttpStatusCode.Unauthorized)
            {
                Logger.Debug("OAuth token expired - refreshing...");
                mToken.Refresh();
                data = Request.Get<Data>(REQUEST_URI, mToken, null);
            }

            if (data.code != HttpStatusCode.OK)
            {
                if (data.code != HttpStatusCode.NoContent)
                    Logger.Error("Failed to fetch Now Playing data: {0}", data.code);

                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;

                return;
            }

            if (data.item == null)
            {
                Logger.Warning("No track item received");
                return;
            }

            mDataAccessMutex.WaitOne();

            // Track change
            if ((mCurrentData == null) || (data.item.id != mCurrentData.item.id))
            {
                mCurrentData = data;
                mChangeExpected = false;
                OnTrackChanged(TrackChangedArgs.FromDataItem(mCurrentData.item));
            }

            // State read - mCurrentData is valid at this point
            StateUpdateArgs state = StateUpdateArgs.FromData(data);

            // Update internal logic according to state
            if (state.State == State.Playing)
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
                OnStateUpdate(state);
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
                    Logger.Debug("Shutdown event triggered - closing");
                    break;
                }

                // no signal means no shutdown requested - continue as normal
                try
                {
                    FetchData();
                }
                catch (Exception e)
                {
                    Logger.Warning("Caught exception while fetching data: {0}. Ignoring and continuing anyway...", e.Message);
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
