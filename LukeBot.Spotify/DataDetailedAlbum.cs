using System.Collections.Generic;


namespace LukeBot.Spotify
{
    public class DataCopyright
    {
        public string text { get; set; }
        public string type { get; set; }

        public DataCopyright()
        {
        }

        public DataCopyright(string text, string type)
        {
            this.text = text;
            this.type = type;
        }
    };

    public class DataDetailedAlbum: Auth.Response
    {
        public List<DataCopyright> copyrights { get; set; }
    }
}