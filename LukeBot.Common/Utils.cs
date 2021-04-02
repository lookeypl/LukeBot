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
            Logger.Secure("GET request response: {0}", retContentStrTask.Result);
            return JsonSerializer.Deserialize<TResp>(retContentStrTask.Result);
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
    }
}
