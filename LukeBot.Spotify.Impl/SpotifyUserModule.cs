using System;
using System.IO;
using System.Net;
using LukeBot.Logging;
using LukeBot.API;
using LukeBot.Config;
using LukeBot.Spotify;
using CommonConstants = LukeBot.Common.Constants;
using System.Net.Http;
using System.Security;
using System.Collections.Generic;


namespace LukeBot.Spotify.Impl
{
    public class SpotifyUserModule: ISpotifyUserModule
    {
        internal string LBUser { get; private set; }

        private string mSpotifyUsername;
        private Token mToken;
        private API.Spotify.UserProfile mProfile;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;
        private object mImplLock = new();

        private Config.Path GetSpotifyUsernameConfigPath()
        {
            return Config.Path.Start()
                    .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                    .Push(LBUser)
                    .Push(CommonConstants.SPOTIFY_SERVICE_NAME)
                    .Push(CommonConstants.PROP_STORE_LOGIN_PROP);
        }

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

        private void Login()
        {
            // TODO should also be from Config...
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify, LBUser);
            mToken.SetScope(new List<string>
            {
                "user-read-currently-playing",
                "user-read-playback-state",
                "user-modify-playback-state",
                "user-read-email"
            });

            if (!mToken.Loaded)
                mToken.Request();

            if (!CheckIfLoginSuccessful())
            {
                throw new InvalidOperationException("Failed to login to Spotify");
            }
        }


        // Public methods //

        public SpotifyUserModule(string lbUser)
        {
            LBUser = lbUser;

            string storagePath = "Outputs/" + CommonConstants.SPOTIFY_SERVICE_NAME + "/" + LBUser;
            Directory.CreateDirectory(storagePath);

            mSpotifyUsername = Conf.Get<string>(GetSpotifyUsernameConfigPath());

            Login();

            mNowPlaying = new NowPlaying(LBUser, mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                LBUser,
                "Outputs/" + CommonConstants.SPOTIFY_SERVICE_NAME + "/" + LBUser + "/nowplaying_artist.txt",
                "Outputs/" + CommonConstants.SPOTIFY_SERVICE_NAME + "/" + LBUser +  "/nowplaying_title.txt"
            );
        }

        ~SpotifyUserModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;
        }

        // returns artist-title of added track if successful; throws on errors
        public TrackData AddSongToQueue(string url)
        {
            lock (mImplLock)
            {
                Logger.Log().Debug("Adding {0} to play queue", url);

                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception)
                {
                    throw new InvalidSpotifyURLException(url);
                }

                // some error checking just in case
                // example URL: https://open.spotify.com/track/2aWm2jIf91nByHThBYNppw?si=add63868785b4a36
                if (!uri.Host.Equals("open.spotify.com"))
                {
                    throw new InvalidSpotifyURLException(url);
                }

                if (uri.Segments.Length != 3 || !uri.Segments[1].Equals("track/"))
                {
                    throw new InvalidSpotifyURLException(url);
                }

                string trackID = uri.Segments[2];
                API.Spotify.Track track = API.Spotify.GetTrack(mToken, trackID);
                if (track.code != HttpStatusCode.OK)
                {
                    Logger.Log().Error("Failed to get Track from Spotify: {0}", track.code);
                    throw new SpotifyQueueAddFailedException(track.code);
                }

                Response resp = API.Spotify.AddItemToPlaybackQueue(mToken, trackID);
                if (!resp.IsSuccess)
                {
                    Logger.Log().Error("Failed to add Track to queue: {0}", resp.code);
                    throw new SpotifyQueueAddFailedException(resp.code);
                }

                TrackData returnData = new (track.artists[0].name, track.name);

                Logger.Log().Debug("Added {0} - {1} to play queue successfully", returnData.Artist, returnData.Title);
                return returnData;
            }
        }

        public void UpdateLogin(string newLogin)
        {
            // TODO
            throw new NotImplementedException("Updating login for Spotify modules not yet implemented");
        }

        public void RenewAuthToken()
        {
            // first stop the now playing thread
            if (mNowPlaying != null && mNowPlaying.IsRunning)
            {
                mNowPlaying.RequestShutdown();
                mNowPlaying.Wait();
                mNowPlaying.Dispose();
                mNowPlaying = null;
            }

            mToken = null;
            AuthManager.Instance.InvalidateToken(ServiceType.Spotify, LBUser);
            Login();

            mNowPlaying = new NowPlaying(LBUser, mToken);
        }

        public void Run()
        {
            mNowPlaying.Run();
        }

        public void RequestShutdown()
        {
            if (mNowPlaying != null)
            {
                mNowPlaying.RequestShutdown();
                mNowPlayingTextFile.Cleanup();
            }
        }

        public void WaitForShutdown()
        {
            if (mNowPlaying != null)
            {
                mNowPlaying.Wait();
                mNowPlaying.Dispose();
                mNowPlaying = null;
            }
        }

        public string GetModuleType()
        {
            return CommonConstants.SPOTIFY_SERVICE_NAME;
        }
    }
}
