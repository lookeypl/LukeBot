using System;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Common.OAuth;


namespace LukeBot.Spotify
{
    public class SpotifyModule : IModule
    {
        private readonly string GET_PROFILE_URI = "https://api.spotify.com/v1/me";

        private Token mToken;
        private NowPlaying mNowPlaying;

        private List<IWidget> mWidgets;
        private string mWidgetID;

        private class UserEmailResponse: Response
        {
            public string email { get; set; }
        };

        bool CheckIfLoginSuccessful()
        {
            UserEmailResponse testResponse = Utils.GetRequest<UserEmailResponse>(GET_PROFILE_URI, mToken, null);
            if (testResponse.code == HttpStatusCode.OK)
                return true;
            else if (testResponse.code == HttpStatusCode.Unauthorized)
                return false;
            else
                throw new LoginFailedException("Failed to login to Spotify service - received other error: " + testResponse.code.ToString());
        }

        void Login()
        {
            string scope = "user-read-currently-playing user-read-playback-state user-read-email";
            mToken = new OAuth.SpotifyToken(AuthFlow.AuthorizationCode);

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
        }

        ~SpotifyModule()
        {
        }

        public void Init()
        {
            Login();

            mNowPlaying = new NowPlaying(mToken);

            // TODO Temporary
            IWidget widget = new NowPlayingWidget(mToken);
            mWidgets.Add(widget);
            mWidgetID = WidgetManager.Instance.Register(widget);
            Logger.Info("Registered NowPlaying widget at link http://localhost:5000/widget/{0}", mWidgetID);
        }

        public void RequestShutdown()
        {

        }

        public void Run()
        {

        }

        public void Wait()
        {

        }
    }
}
