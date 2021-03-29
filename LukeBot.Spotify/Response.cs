using System.Net;
using LukeBot.Common;

namespace LukeBot.Spotify
{
    class Response
    {
        public HttpStatusCode code { get; set; }

        public Response()
        {
            code = HttpStatusCode.OK;
        }
    }
}
