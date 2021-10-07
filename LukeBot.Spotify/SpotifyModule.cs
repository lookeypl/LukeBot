using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Spotify
{
    public class SpotifyModule : IModule
    {
        private readonly string GET_PROFILE_URI = "https://api.spotify.com/v1/me";

        private Token mToken;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;

        private List<IWidget> mWidgets;

        private class UserEmailResponse: Response
        {
            public string email { get; set; }
        };

        bool CheckIfLoginSuccessful()
        {
            UserEmailResponse testResponse = Request.Get<UserEmailResponse>(GET_PROFILE_URI, mToken, null);
            if (testResponse.code == HttpStatusCode.OK)
            {
                Logger.Debug("Spotify login successful");
                return true;
            }
            else if (testResponse.code == HttpStatusCode.Unauthorized)
            {
                Logger.Error("Failed to login to Spotify - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Spotify service: " + testResponse.code.ToString());
        }

        void Login()
        {
            string scope = "user-read-currently-playing user-read-playback-state user-read-email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify);

            bool tokenFromFile = mToken.Loaded;

            if (!mToken.Loaded)
                mToken.Request(scope);

            if (!CheckIfLoginSuccessful())
            {
                if (tokenFromFile)
                {
                    mToken.Refresh();
                    if (!CheckIfLoginSuccessful())
                    {
                        mToken.Remove();
                        throw new InvalidOperationException(
                            "Failed to refresh OAuth Token. Token has been removed, restart to re-login and request a fresh OAuth token"
                        );
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Spotify");
            }
        }

        public SpotifyModule()
        {
            mWidgets = new List<IWidget>();
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);
            string storagePath = "Outputs/" + Constants.SERVICE_NAME;
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);
        }

        ~SpotifyModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;

            foreach (IWidget w in mWidgets)
                WidgetManager.Instance.Unregister(w);
        }

        public void Init()
        {
            Login();

            mNowPlaying = new NowPlaying(mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(mNowPlaying,
                "Outputs/" + Constants.SERVICE_NAME + "/nowplaying_artist.txt",
                "Outputs/" + Constants.SERVICE_NAME + "/nowplaying_title.txt"
            );

            // TODO Temporary
            NowPlayingWidget widget = new NowPlayingWidget(mNowPlaying);
            mWidgets.Add(widget);
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
