using System;
using System.IO;
using System.Threading;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Core;
using LukeBot.Core.Events;
using LukeBot.Spotify.Common;


namespace LukeBot.Widget
{
    class NowPlaying: IWidget
    {
        ConnectionPort mPort;
        SpotifyMusicStateUpdateArgs mState;
        SpotifyMusicTrackChangedArgs mCurrentTrack;
        WebSocketServer mServer;
        Thread mRecvThread;
        volatile bool mDone = false;

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyMusicStateUpdateArgs a = (SpotifyMusicStateUpdateArgs)args;

            mState = a;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(a));
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyMusicTrackChangedArgs a = (SpotifyMusicTrackChangedArgs)args;

            mCurrentTrack = a;
            if (mServer.Running)
                mServer.Send(JsonSerializer.Serialize(a));
        }

        public NowPlaying(string widgetID)
            : base()
        {
            mPort = Systems.Connection.AcquirePort();
            Logger.Log().Debug("Widget will have port {0}", mPort.Value);

            string serverIP = LukeBot.Common.Utils.GetConfigServerIP();
            AddToHead(string.Format("<meta name=\"serveraddress\" content=\"{0}\">", serverIP + ":" + mPort.Value));

            mServer = new WebSocketServer(serverIP, mPort.Value);

            //Systems.Widget.Register(this, widgetID);
            Logger.Log().Secure("Registered Chat widget at link http://{0}/widget/{1}; WS port {2}", serverIP, ID, mPort.Value);

            mState = null;
            mCurrentTrack = null;

            Systems.Event.SpotifyMusicStateUpdate += OnStateUpdate;
            Systems.Event.SpotifyMusicTrackChanged += OnTrackChanged;
        }

        ~NowPlaying()
        {
        }

        // TODO remove this thread, instead add OnConnected event to WebSocketServer
        // and react there with state/track update
        public void ThreadMain()
        {
            mServer.Start();
            mServer.AwaitConnection();

            if (mState != null && mState.State != PlayerState.Unloaded)
            {
                // Push a state update to "pre-refresh" the widget
                OnTrackChanged(null, mCurrentTrack);
                OnStateUpdate(null, mState);
            }

            while (!mDone)
            {
                try {
                    WebSocketMessage msg1 = mServer.Recv();
                    Logger.Log().Debug("Received message: {0}", msg1.TextMessage);
                    if (msg1.Opcode == WebSocketOp.Close)
                    {
                        mDone = true;
                    }
                } catch (System.Exception e) {
                    Logger.Log().Warning("Exception caught: {0}: {1}", e.GetType().ToString(), e.Message);
                }
            }

            Logger.Log().Debug("NowPlaying Widget server thread closed");
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
