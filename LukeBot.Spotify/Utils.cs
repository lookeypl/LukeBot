using System.Net;
using LukeBot.Spotify.Common;
using LukeBot.Core.Events;


namespace LukeBot.Spotify
{
    public class Utils
    {
        public static SpotifyMusicTrackChangedArgs DataItemToTrackChangedArgs(DataItem item)
        {
            float duration = item.duration_ms / 1000.0f; // convert to seconds
            string title = item.name;
            string artists = item.artists[0].name;
            for (int i = 1; i < item.artists.Count; ++i)
            {
                artists += ", ";
                artists += item.artists[i].name;
            }

            string label = "";
            bool labelFilled = false;
            // try looking for publisher (type "P")
            foreach (DataCopyright c in item.album.copyrights)
            {
                if (c.type == "P")
                {
                    label = c.text;
                    labelFilled = true;
                    break;
                }
            }

            // if failed - try looking for copyright (type "C")
            if (!labelFilled)
            {
                foreach (DataCopyright c in item.album.copyrights)
                {
                    if (c.type == "C")
                    {
                        label = c.text;
                        labelFilled = true;
                        break;
                    }
                }
            }

            // if we succeeded earlier, reformat the extracted copyright info
            if (labelFilled)
                label = '[' + label + ']';
            else
                label = "[???]";

            SpotifyMusicTrackChangedArgs ret = new SpotifyMusicTrackChangedArgs(artists, title, label, duration);

            return ret;
        }

        public static SpotifyMusicStateUpdateArgs DataToStateUpdateArgs(Data data)
        {
            float progress;
            PlaybackState state;

            if (data.code == HttpStatusCode.NoContent)
            {
                progress = 0.0f;
                state = PlaybackState.Unloaded;
            }
            else
            {
                progress = data.progress_ms / 1000.0f; // conversion from ms to s
                state = data.is_playing ? PlaybackState.Playing : PlaybackState.Stopped;
            }

            SpotifyMusicStateUpdateArgs ret = new SpotifyMusicStateUpdateArgs(state, progress);
            return ret;
        }
    }
}