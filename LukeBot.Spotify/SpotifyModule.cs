using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Core;


namespace LukeBot.Spotify
{
    public class SpotifyModule : IModule
    {
        private Token mToken;
        private API.Spotify.UserProfile mProfile;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;

        private List<IWidget> mWidgets;

        bool CheckIfLoginSuccessful()
        {
            mProfile = API.Spotify.GetCurrentUserProfile(mToken);
            if (mProfile.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Spotify login successful");
                return true;
            }
            else if (mProfile.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Spotify - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Spotify service: " + mProfile.code.ToString());
        }

        void Login()
        {
            string scope = "user-read-currently-playing user-read-playback-state user-read-email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify, "lookey");

            bool tokenFromFile = mToken.Loaded;

            if (!mToken.Loaded)
                mToken.Request(scope);

            if (!CheckIfLoginSuccessful())
            {
                throw new InvalidOperationException("Failed to login to Spotify");
            }
        }

        public SpotifyModule()
        {
            mWidgets = new List<IWidget>();
            Systems.Communication.Register(Constants.SERVICE_NAME);
            string storagePath = "Outputs/" + Constants.SERVICE_NAME;
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);
        }

        ~SpotifyModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;

            foreach (IWidget w in mWidgets)
                Systems.Widget.Unregister(w);
        }

        public void Init()
        {
            Login();

            mNowPlaying = new NowPlaying(mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                "Outputs/" + Constants.SERVICE_NAME + "/nowplaying_artist.txt",
                "Outputs/" + Constants.SERVICE_NAME + "/nowplaying_title.txt"
            );

            // TODO Temporary
            mWidgets.Add(new NowPlayingWidget("BIG-NOW-PLAYING-WIDGET"));
            mWidgets.Add(new NowPlayingWidget("SMALL-NOW-PLAYING-WIDGET"));
        }

        public void RequestShutdown()
        {
            mNowPlaying.RequestShutdown();
            mNowPlayingTextFile.Cleanup();
            foreach (IWidget w in mWidgets)
                w.RequestShutdown();
        }

        public void Run()
        {
            mNowPlaying.Run();
        }

        public void WaitForShutdown()
        {
            mNowPlaying.Wait();
            foreach (IWidget w in mWidgets)
                w.WaitForShutdown();
        }
    }
}
