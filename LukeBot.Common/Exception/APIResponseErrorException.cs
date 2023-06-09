using System.Net;

namespace LukeBot.Common
{
    public class APIResponseErrorException: Exception
    {
        public APIResponseErrorException(HttpStatusCode code)
            : base(string.Format("API Responded with HTTP code {0}", code.ToString()))
        {}
    }
}
