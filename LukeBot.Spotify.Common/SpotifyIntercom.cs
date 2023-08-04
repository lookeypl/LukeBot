using Intercom = LukeBot.Communication.Common.Intercom;


namespace LukeBot.Spotify.Common
{
    public class Endpoints
    {
        public const string SPOTIFY_MAIN_MODULE = "SpotifyMainModule";
    }

    // Messages

    public class Messages
    {
        public const string ADD_SONG_TO_QUEUE = "AddSongToQueue";
    }

    public class IntercomMessageBase: Intercom::MessageBase
    {
        // LukeBot user that this message is about
        public string User { get; set; }

        public IntercomMessageBase(string endpoint, string msgType)
            : base(endpoint, msgType)
        {
        }
    }

    public class AddSongToQueueMsg: IntercomMessageBase
    {
        public string URL { get; set; }

        public AddSongToQueueMsg()
            : base(Endpoints.SPOTIFY_MAIN_MODULE, Messages.ADD_SONG_TO_QUEUE)
        {
        }
    }

    // Responses

    public class AddSongToQueueResponse: Intercom::ResponseBase
    {
        public string Artist { get; set; }
        public string Title { get; set; }

        public AddSongToQueueResponse()
            : base()
        {
        }
    }
}
