using System.Net;
using System.Collections.Generic;

namespace LukeBot.Common
{
    class HTTPResponse
    {
        public HTTPRequest.RequestVersion Version { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public Dictionary<string, string> Headers { get; set; }

        private string RequestVersionToString(HTTPRequest.RequestVersion version)
        {
            switch (version)
            {
            case HTTPRequest.RequestVersion.HTTP10: return "HTTP/1.0";
            case HTTPRequest.RequestVersion.HTTP11: return "HTTP/1.1";
            default:
                throw new ParsingErrorException("Invalid HTTP version");
            }
        }

        public string GetAsString()
        {
            const string EOL = "\r\n";
            const string HEADER_DELIM = ": ";

            // Prepare first line
            string r = RequestVersionToString(Version) + ' ';
            r += Utils.HttpStatusCodeToHTTPString(StatusCode) + EOL;

            // Add HTTP headers
            foreach (var h in Headers)
            {
                r += h.Key + HEADER_DELIM + h.Value + EOL;
            }

            // Final newline to end the HTTP response
            r += EOL;

            return r;
        }

        // Forms a HTTP Response. Headers property is initialized to be non-NULL, however it
        // requires to be manually filled in.
        public static HTTPResponse FormResponse(HTTPRequest.RequestVersion version, HttpStatusCode code)
        {
            HTTPResponse r = new HTTPResponse();

            r.Version = version;
            r.StatusCode = code;
            r.Headers = new Dictionary<string, string>();

            return r;
        }
    }
}
