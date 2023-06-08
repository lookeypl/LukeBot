using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Module;


namespace LukeBot.Spotify
{
    public class SpotifyModule : IMainModule
    {
        private string mLBUser;
        private Token mToken;
        private API.Spotify.UserProfile mProfile;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;


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

        void Login(string username)
        {
            string scope = "user-read-currently-playing user-read-playback-state user-read-email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify, username);

            bool tokenFromFile = mToken.Loaded;

            if (!mToken.Loaded)
                mToken.Request(scope);

            if (!CheckIfLoginSuccessful())
            {
                throw new InvalidOperationException("Failed to login to Spotify");
            }
        }

        // User Module Descriptor delegates //

        private bool UserModuleLoadPrerequisites()
        {
            // TODO
            return true;
        }

        private IUserModule UserModuleLoader(string lbUser)
        {
            // TODO
            return null;
        }


        // Public methods //

        public SpotifyModule(string lbUser)
        {
            mLBUser = lbUser;

            Comms.Communication.Register(LukeBot.Common.Constants.SPOTIFY_MODULE_NAME);
            string storagePath = "Outputs/" + LukeBot.Common.Constants.SPOTIFY_MODULE_NAME;
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            string spotifyUsername = Conf.Get<string>(
                LukeBot.Common.Utils.FormConfName(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN, mLBUser, Constants.PROP_STORE_SPOTIFY_DOMAIN, Constants.PROP_STORE_SPOTIFY_LOGIN)
            );

            Login(spotifyUsername);

            mNowPlaying = new NowPlaying(mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                "Outputs/" + LukeBot.Common.Constants.SPOTIFY_MODULE_NAME + "/" + mLBUser + "/nowplaying_artist.txt",
                "Outputs/" + LukeBot.Common.Constants.SPOTIFY_MODULE_NAME + "/" + mLBUser +  "/nowplaying_title.txt"
            );
        }

        ~SpotifyModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Spotify;
            umd.LoadPrerequisite = null;
            umd.Loader = UserModuleLoader;
            return umd;
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
    }
}
