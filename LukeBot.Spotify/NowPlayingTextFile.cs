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

        private void WriteToFile(string path, string text)
        {
            FileStream file = File.Open(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(text);
            writer.Close();
            file.Close();
        }

        private void OnTrackChanged(object o, NowPlaying.TrackChangedArgs args)
        {
            try
            {
                mCurrentTrack = args;
                mNeedsUpdate = true;
                Logger.Debug("Track {0}", mCurrentTrack);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process track change: {0}", e.Message);
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
                    Logger.Debug("Clearing files");
                    // Open files with Create mode to clear them
                    File.Open(mArtistFilePath, FileMode.Create).Close();
                    File.Open(mTitleFilePath, FileMode.Create).Close();
                    mNeedsUpdate = true;
                    break;
                case NowPlaying.State.Playing:
                    if (mNeedsUpdate)
                    {
                        Logger.Debug("Updating files with data {0}", mCurrentTrack);
                        WriteToFile(mArtistFilePath, mCurrentTrack.Artists);
                        WriteToFile(mTitleFilePath, mCurrentTrack.Title);
                        mNeedsUpdate = false;
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process state change: {0}", e.Message);
            }
        }
    }
}
