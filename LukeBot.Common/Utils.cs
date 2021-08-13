using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace LukeBot.Common
{
    public class Utils
    {
        public static TResp GetRequest<TResp>(string uri, OAuth.Token token, Dictionary<string, string> query) where TResp: Response, new()
        {
            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (query != null && query.Count > 0)
                request.Content = new FormUrlEncodedContent(query);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Task<HttpResponseMessage> responseTask = client.SendAsync(request);
            responseTask.Wait(30 * 1000);
            HttpResponseMessage response = responseTask.Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                TResp r = new TResp();
                r.code = response.StatusCode;
                return r;
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return JsonSerializer.Deserialize<TResp>(retContentStrTask.Result);
        }

        public static string HttpStatusCodeToHTTPString(HttpStatusCode code)
        {
            // TODO not all codes are filled in cause I'm lazy. Maybe ArgumentException is thrown
            // because I was code is not on the list below. Fill it in some day.
            switch (code)
            {
            // 100s
            case HttpStatusCode.Continue: return "100 Continue";
            case HttpStatusCode.SwitchingProtocols: return "101 Switching Protocols";
            case HttpStatusCode.Processing: return "102 Processing";
            case HttpStatusCode.EarlyHints: return "103 Early Hints";
            // 200s
            case HttpStatusCode.OK: return "200 OK";
            case HttpStatusCode.Created: return "201 Created";
            case HttpStatusCode.Accepted: return "202 Accepted";
            case HttpStatusCode.NonAuthoritativeInformation: return "203 Non-Authoritative Information";
            case HttpStatusCode.NoContent: return "204 No Content";
            // 300s
            // 400s
            case HttpStatusCode.BadRequest: return "400 Bad Request";
            case HttpStatusCode.Unauthorized: return "401 Unauthorized";
            case HttpStatusCode.PaymentRequired: return "402 Payment Required";
            case HttpStatusCode.Forbidden: return "403 Forbidden";
            case HttpStatusCode.NotFound: return "404 Not Found";
            case HttpStatusCode.RequestTimeout: return "408 Request Timeout";
            case HttpStatusCode.Gone: return "410 Gone";
            // 500s
            case HttpStatusCode.InternalServerError: return "500 Internal Server Error";
            case HttpStatusCode.NotImplemented: return "501 Not Implemented";
            case HttpStatusCode.BadGateway: return "502 Bad Gateway";
            case HttpStatusCode.ServiceUnavailable: return "503 Service Unavailable";
            case HttpStatusCode.HttpVersionNotSupported: return "505 HTTP Version Not Supported";
            default:
                throw new ArgumentException(string.Format("Unsupported HTTP status code: {0}", code));
            }
        }

        public static Process StartBrowser(string url)
        {
            Process result = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = Process.Start("xdg-open", url);
            }
            else
            {
                throw new UnsupportedPlatformException("Platform is not supported");
            }

            return result;
        }

        // TODO temporary until PropertyStore shows up
        public static string GetConfigServerIP()
        {
            string ret;
            if (!DataFileReader.Instance.Get(Constants.SERVER_IP_FILE, out ret))
                ret = Constants.DEFAULT_SERVER_IP;

            return ret;
        }
    }
}
