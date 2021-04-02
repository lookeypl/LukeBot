using System.Net;

namespace LukeBot.Common
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
