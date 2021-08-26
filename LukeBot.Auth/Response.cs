using System.Net;

// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.Auth
{
    public class Response
    {
        public HttpStatusCode code { get; set; }

        public Response()
        {
            code = HttpStatusCode.OK;
        }
    }
}
