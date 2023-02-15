using System;
using System.IO;
using LukeBot.Common;
using LukeBot.Spotify.Common;
using LukeBot.Communication;
using LukeBot.Communication.Events;


namespace LukeBot.Spotify
{
    class NowPlayingTextFile
    {
        private string mArtistFilePath;
        private string mTitleFilePath;
        private bool mNeedsUpdate;
        private SpotifyMusicTrackChangedArgs mCurrentTrack;

        public NowPlayingTextFile(string artistPath, string titlePath)
        {
            mArtistFilePath = artistPath;
            mTitleFilePath = titlePath;
            mNeedsUpdate = false;

            Comms.Event.SpotifyMusicStateUpdate += OnStateUpdate;
            Comms.Event.SpotifyMusicTrackChanged += OnTrackChanged;
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
            SpotifyMusicTrackChangedArgs a = (SpotifyMusicTrackChangedArgs)args;

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
            SpotifyMusicStateUpdateArgs a = (SpotifyMusicStateUpdateArgs)args;

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
