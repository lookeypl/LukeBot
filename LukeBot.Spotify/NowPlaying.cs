using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Common.OAuth;

namespace LukeBot.Spotify
{
    class NowPlaying
    {
        class NowPlayingData
        {

        };

        private readonly int DEFAULT_EVENT_TIMEOUT = 5 * 1000; // 5 seconds
        private readonly string REQUEST_URI = "https://api.spotify.com/v1/me/player";

        private Token mToken;
        private Thread mThread;
        private Mutex mDataAccessMutex;
        private ManualResetEvent mShutdownEvent;
        private int mEventTimeout;
        private bool mRefreshed;

        public NowPlaying(Token token)
        {
            mToken = token;
            mThread = new Thread(new ThreadStart(ThreadMain));
            mDataAccessMutex = new Mutex();
            mShutdownEvent = new ManualResetEvent(false);
            mEventTimeout = DEFAULT_EVENT_TIMEOUT;
            mRefreshed = false;
        }

        ~NowPlaying()
        {
        }

        void FetchData()
        {
            //NowPlayingData data = Utils.GetRequest<NowPlayingData>(REQUEST_URI, mToken
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
                FetchData();
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
    }
}
