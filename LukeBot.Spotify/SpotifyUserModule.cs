using System;
using System.IO;
using System.Net;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Config;
using LukeBot.Module;
using CommonUtils = LukeBot.Common.Utils;
using CommonConstants = LukeBot.Common.Constants;


namespace LukeBot.Spotify
{
    public class SpotifyUserModule: IUserModule
    {
        internal string LBUser { get; private set; }
        private string mSpotifyUsername;
        private Token mToken;
        private API.Spotify.UserProfile mProfile;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;


        private bool CheckIfLoginSuccessful()
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

        private void Login(string username)
        {
            string scope = "user-read-currently-playing user-read-playback-state user-read-email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify, LBUser );

            bool tokenFromFile = mToken.Loaded;

            if (!mToken.Loaded)
                mToken.Request(scope);

            if (!CheckIfLoginSuccessful())
            {
                throw new InvalidOperationException("Failed to login to Spotify");
            }
        }


        // Public methods //

        public SpotifyUserModule(string lbUser)
        {
            LBUser = lbUser;

            string storagePath = "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser;
            Directory.CreateDirectory(storagePath);

            mSpotifyUsername = Conf.Get<string>(
                CommonUtils.FormConfName(CommonConstants.PROP_STORE_USER_DOMAIN, LBUser, CommonConstants.SPOTIFY_MODULE_NAME, Constants.PROP_STORE_SPOTIFY_LOGIN_PROP)
            );

            Login(mSpotifyUsername);

            mNowPlaying = new NowPlaying(mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser + "/nowplaying_artist.txt",
                "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser +  "/nowplaying_title.txt"
            );
        }

        ~SpotifyUserModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;
        }

        public void Run()
        {
            mNowPlaying.Run();
        }

        public void RequestShutdown()
        {
            mNowPlaying.RequestShutdown();
            mNowPlayingTextFile.Cleanup();
        }

        public void WaitForShutdown()
        {
            mNowPlaying.Wait();
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Spotify;
        }
    }
}
