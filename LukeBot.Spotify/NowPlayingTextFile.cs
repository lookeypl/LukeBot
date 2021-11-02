using System;
using System.IO;
using LukeBot.Common;

namespace LukeBot.Spotify
{
    class NowPlayingTextFile
    {
        private string mArtistFilePath;
        private string mTitleFilePath;
        private bool mNeedsUpdate;
        private NowPlaying.TrackChangedArgs mCurrentTrack;

        public NowPlayingTextFile(NowPlaying engine, string artistPath, string titlePath)
        {
            mArtistFilePath = artistPath;
            mTitleFilePath = titlePath;
            mNeedsUpdate = false;

            engine.TrackChanged += OnTrackChanged;
            engine.StateUpdate += OnStateUpdate;
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

        private void OnTrackChanged(object o, NowPlaying.TrackChangedArgs args)
        {
            try
            {
                mCurrentTrack = args;
                mNeedsUpdate = true;
            }
            catch (Exception e)
            {
                Logger.Log().Error("Failed to process track change: {0}", e.Message);
            }
        }

        private void OnStateUpdate(object o, NowPlaying.StateUpdateArgs args)
        {
            try
            {
                switch (args.State)
                {
                case NowPlaying.State.Unloaded:
                case NowPlaying.State.Stopped:
                    Logger.Log().Debug("Playback stopped/unloaded - clearing files");
                    // Open files with Create mode to clear them
                    ClearFile(mArtistFilePath);
                    ClearFile(mTitleFilePath);
                    mNeedsUpdate = true;
                    break;
                case NowPlaying.State.Playing:
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
            catch (Exception e)
            {
                Logger.Log().Error("Failed to process state change: {0}", e.Message);
            }
        }
    }
}
