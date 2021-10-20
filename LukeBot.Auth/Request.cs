using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.Auth
{
    public class Request
    {
        public static TResp Get<TResp>(string uri, Token token, Dictionary<string, string> query) where TResp: Response, new()
        {
            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (query != null && query.Count > 0)
                request.Content = new FormUrlEncodedContent(query);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
            request.Headers.Add("Client-Id", token.ClientID);
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
    }
}
