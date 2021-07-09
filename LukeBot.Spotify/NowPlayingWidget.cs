using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using LukeBot.Common;
using LukeBot.Common.OAuth;


namespace LukeBot.Spotify
{
    class NowPlayingWidget: IWidget
    {
        NowPlaying mEngine;
        ConnectionPort mPort;
        NowPlaying.TrackChangedArgs mCurrentTrack;
        WebSocketServer mServer;


        private void OnTrackChanged(object o, NowPlaying.TrackChangedArgs args)
        {
            mCurrentTrack = args;
            mServer.Send(mCurrentTrack.ToString());
        }

        public NowPlayingWidget(NowPlaying engine)
            : base()
        {
            mEngine = engine;

            mPort = ConnectionManager.Instance.AcquirePort();
            Logger.Debug("Widget will have port {0}", mPort.Value);

            mEngine.TrackChanged += OnTrackChanged;

            AddToHead(string.Format("<meta name=\"widgetport\" content=\"{0}\">", mPort.Value));

            // TEMPORARY - should be generated and added to OBS maybe?
            AddToHead(
                "<style>" +
                "body {" +
                "    background-color: rgba(0, 0, 0, 0);" +
                "    margin: 0px auto;" +
                "    overflow: hidden;" +
                "    color: #ffffff;" +
                "    font: 48px Arial;" +
                "    -webkit-text-stroke-width: 1px;" +
                "    -webkit-text-stroke-color: black;" +
                "}" +
                ".debug {" +
                "    font: 16px Arial;" +
                "    -webkit-text-stroke-width: 0px;" +
                "    color: #ffffff;" +
                "}" +
                "</style>"
            );

            mServer = new WebSocketServer("127.0.0.1", mPort.Value);
            mServer.Start();
        }

        ~NowPlayingWidget()
        {
        }

        public void Test()
        {
            mServer.AwaitConnection();
            WebSocketMessage msg1 = mServer.Recv();
            Logger.Debug("Received message: {0}", msg1.TextMessage);
            WebSocketMessage msg2 = mServer.Recv();
            Logger.Debug("Received 2 message: {0}", msg2.TextMessage);
            mServer.Send(msg1);
            mServer.Send(msg2);
            mServer.Send("Oh my god is this thing actually working");
        }

        protected override string GetWidgetCode()
        {
            StreamReader reader = File.OpenText("LukeBot.Spotify/Widgets/NowPlaying.html");
            string p = reader.ReadToEnd();
            reader.Close();
            return p;
        }

        public override void RequestShutdown()
        {
            mServer.RequestShutdown();
        }

        public override void WaitForShutdown()
        {
            mServer.WaitForShutdown();
        }
    }
}
