using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LukeBot.Common;
using LukeBot.Common.OAuth;


namespace LukeBot.Spotify
{
    class NowPlayingWidget: IWidget
    {
        NowPlaying mEngine;
        ConnectionPort mPort;
        NowPlaying.StateUpdateArgs mState;
        NowPlaying.TrackChangedArgs mCurrentTrack;
        WebSocketServer mServer;
        Thread mRecvThread;
        volatile bool mDone = false;

        private void OnStateUpdate(object o, NowPlaying.StateUpdateArgs args)
        {
            mState = args;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(args));
        }

        private void OnTrackChanged(object o, NowPlaying.TrackChangedArgs args)
        {
            mCurrentTrack = args;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(args));
        }

        public NowPlayingWidget(NowPlaying engine)
            : base()
        {
            mEngine = engine;

            mPort = ConnectionManager.Instance.AcquirePort();
            Logger.Debug("Widget will have port {0}", mPort.Value);

            mEngine.TrackChanged += OnTrackChanged;
            mEngine.StateUpdate += OnStateUpdate;

            AddToHead(string.Format("<meta name=\"widgetport\" content=\"{0}\">", mPort.Value));

            mServer = new WebSocketServer("127.0.0.1", mPort.Value);
            mState = null;
            mCurrentTrack = null;
        }

        ~NowPlayingWidget()
        {
        }

        public void ThreadMain()
        {
            mServer.Start();
            mServer.AwaitConnection();

            if (mState != null && mState.State != NowPlaying.State.Unloaded)
                OnTrackChanged(null, mCurrentTrack); // to send over currently playing track

            while (!mDone)
            {
                try {
                    WebSocketMessage msg1 = mServer.Recv();
                    Logger.Debug("Received message: {0}", msg1.TextMessage);
                } catch (Exception e) {
                    Logger.Warning("Exception caught: {0}: {1}", e.GetType().ToString(), e.Message);
                }
            }
        }

        protected override string GetWidgetCode()
        {
            if (mServer.Running)
            {
                mDone = true;
                mServer.RequestShutdown();
                mServer.WaitForShutdown();
                mRecvThread.Join();
            }

            StreamReader reader = File.OpenText("LukeBot.Spotify/Widgets/NowPlaying.html");
            string p = reader.ReadToEnd();
            reader.Close();

            mDone = false;
            mRecvThread = new Thread(ThreadMain);
            mRecvThread.Start();

            return p;
        }

        public override void RequestShutdown()
        {
            mDone = true;
            mServer.RequestShutdown();
        }

        public override void WaitForShutdown()
        {
            if (mServer.Running)
            {
                mServer.WaitForShutdown();
                mRecvThread.Join();
            }
        }
    }
}
