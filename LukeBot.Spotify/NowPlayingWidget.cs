using System;
using System.IO;
using System.Threading;
using System.Text.Json;
using LukeBot.Common;


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

            string serverIP = Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            string widgetID = WidgetManager.Instance.Register(this, "TEST-WIDGET-ID");
            Logger.Secure("Registered Chat widget at link http://{0}/widget/{1}; WS port {2}", serverIP, ID, mPort.Value);

            mState = null;
            mCurrentTrack = null;
        }

        ~NowPlayingWidget()
        {
        }

        // TODO remove this thread, instead and OnConnected event to WebSocketServer
        // and react there with state/track update
        public void ThreadMain()
        {
            mServer.Start();
            mServer.AwaitConnection();

            if (mState != null && mState.State != NowPlaying.State.Unloaded)
            {
                // Push a state update to "pre-refresh" the widget
                OnTrackChanged(null, mCurrentTrack);
                OnStateUpdate(null, mState);
            }

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
