using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Core;
using LukeBot.Config;


namespace LukeBot.Spotify
{
    public class SpotifyModule : IModule
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

        public SpotifyModule(string lbUser)
        {
            mLBUser = lbUser;

            Systems.Communication.Register(Constants.SERVICE_NAME);
            string storagePath = "Outputs/" + Constants.SERVICE_NAME;
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            string spotifyUsername = Conf.Get<string>(
                LukeBot.Common.Utils.FormConfName(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN, mLBUser, Constants.PROP_STORE_SPOTIFY_DOMAIN, Constants.PROP_STORE_SPOTIFY_LOGIN)
            );

            Login(spotifyUsername);

            mNowPlaying = new NowPlaying(mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                "Outputs/" + Constants.SERVICE_NAME + "/" + mLBUser + "/nowplaying_artist.txt",
                "Outputs/" + Constants.SERVICE_NAME + "/" + mLBUser +  "/nowplaying_title.txt"
            );
        }

        ~SpotifyModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;
        }

        public void Init()
        {
        }

        public void RequestShutdown()
        {
            mNowPlaying.RequestShutdown();
            mNowPlayingTextFile.Cleanup();
        }

        public void Run()
        {
            mNowPlaying.Run();
        }

        public void WaitForShutdown()
        {
            mNowPlaying.Wait();
        }
    }
}
