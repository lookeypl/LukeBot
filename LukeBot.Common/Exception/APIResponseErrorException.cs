using System.Net;

namespace LukeBot.Common
{
    public class APIResponseErrorException: Exception
    {
        public APIResponseErrorException(HttpStatusCode code)
            : base(string.Format("API Responded with HTTP code {0}", code.ToString()))
        {}

        public APIResponseErrorException(HttpStatusCode code, string extraMsg)
            : base(string.Format("API Responded with HTTP code {0} - {1}", code.ToString(), extraMsg))
        {}
    }
}
