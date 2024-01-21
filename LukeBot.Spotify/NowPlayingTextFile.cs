using System.IO;
using LukeBot.Logging;
using LukeBot.Spotify.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;


namespace LukeBot.Spotify
{
    class NowPlayingTextFile
    {
        private string mLBUser;
        private string mArtistFilePath;
        private string mTitleFilePath;
        private bool mNeedsUpdate;
        private SpotifyTrackChangedArgs mCurrentTrack;

        public NowPlayingTextFile(string lbUser, string artistPath, string titlePath)
        {
            mLBUser = lbUser;
            mArtistFilePath = artistPath;
            mTitleFilePath = titlePath;
            mNeedsUpdate = false;

            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_STATE_UPDATE).Endpoint += OnStateUpdate;
            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_TRACK_CHANGED).Endpoint += OnTrackChanged;
        }

        ~NowPlayingTextFile()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            ClearFile(mArtistFilePath);
            ClearFile(mTitleFilePath);
        }

        private void WriteToFile(string path, string text)
        {
            FileStream file = File.Open(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(text);
            writer.Close();
            file.Close();
        }

        private void ClearFile(string path)
        {
            File.Open(path, FileMode.Create).Close();
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyTrackChangedArgs a = (SpotifyTrackChangedArgs)args;

            try
            {
                mCurrentTrack = a;
                mNeedsUpdate = true;
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to process track change: {0}", e.Message);
            }
        }

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyStateUpdateArgs a = (SpotifyStateUpdateArgs)args;

            try
            {
                switch (a.State)
                {
                case PlayerState.Unloaded:
                case PlayerState.Stopped:
                    Logger.Log().Debug("Playback stopped/unloaded - clearing files");
                    // Open files with Create mode to clear them
                    ClearFile(mArtistFilePath);
                    ClearFile(mTitleFilePath);
                    mNeedsUpdate = true;
                    break;
                case PlayerState.Playing:
                    if (mNeedsUpdate)
                    {
                        Logger.Log().Debug("Playing - updating with {0}", mCurrentTrack);
                        WriteToFile(mArtistFilePath, mCurrentTrack.Artists);
                        WriteToFile(mTitleFilePath, mCurrentTrack.Title);
                        mNeedsUpdate = false;
                    }
                    break;
                }
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to process state change: {0}", e.Message);
            }
        }
    }
}
