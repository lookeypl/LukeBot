using System.Collections.Generic;

namespace LukeBot.Common
{
    class HTTPRequest
    {
        public enum RequestType
        {
            Get,
            Post
        };

        public enum RequestVersion
        {
            HTTP10,
            HTTP11,
        };

        public RequestType Type { get; private set; }
        public string Path { get; private set; }
        public RequestVersion Version { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }

        private HTTPRequest()
        {
        }

        private static RequestType GetRequestType(string token)
        {
            switch (token)
            {
            case "GET": return RequestType.Get;
            case "POST": return RequestType.Post;
            default:
                throw new ParsingErrorException(string.Format("Failed to determine HTTP request type: {0}", token));
            }
        }

        private static RequestVersion GetHTTPVersion(string token)
        {
            switch (token)
            {
            case "HTTP/1.0": return RequestVersion.HTTP10;
            case "HTTP/1.1": return RequestVersion.HTTP11;
            default:
                throw new ParsingErrorException(string.Format("Failed to determine HTTP request version: {0}", token));
            }
        }

        public static HTTPRequest Parse(string request)
        {
            HTTPRequest r = new HTTPRequest();

            string[] lines = request.Replace("\r\n", "\n").Split('\n');

            string[] requestStartTokens = lines[0].Split(' ');
            if (requestStartTokens.Length != 3)
                throw new ParsingErrorException("Invalid amount of tokens at start of request");

            r.Type = GetRequestType(requestStartTokens[0]);
            r.Path = requestStartTokens[1];
            r.Version = GetHTTPVersion(requestStartTokens[2]);

            r.Headers = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; ++i)
            {
                int split = lines[i].IndexOf(": ");
                if (split == -1)
                    continue; // ignore unreadable line

                r.Headers.Add(lines[i].Substring(0, split), lines[i].Substring(split + 2));
            }

            return r;
        }
    };
}
