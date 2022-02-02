using System;
using System.Collections.Generic;


namespace LukeBot.Spotify
{
    public class DataArtists
    {
        public string name { get; set; }
    };

    public class DataAlbum
    {
        public string href { get; set; }
        public List<DataCopyright> copyrights { get; set; }
    };

    public class DataItem
    {
        public List<DataArtists> artists { get; set; }
        public DataAlbum album { get; set; }
        public int duration_ms { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    };

    public class Data: Auth.Response
    {
        public DataItem item { get; set; }
        public bool is_playing { get; set; }
        public int progress_ms { get; set; }

        public override string ToString()
        {
            string artists = item.artists[0].name;
            for (int i = 1; i < item.artists.Count; ++i)
            {
                artists += ", ";
                artists += item.artists[i].name;
            }
            return String.Format("{0} - {1} ({2}/{3})", artists, item.name,
                                    progress_ms / 1000.0f, item.duration_ms / 1000.0f);
        }
    };
}
