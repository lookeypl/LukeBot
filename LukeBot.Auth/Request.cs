using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;


// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.Auth
{
    public class Request
    {
        public static TResp Get<TResp>(string uri, Token token, Dictionary<string, string> query) where TResp: Response, new()
        {
            HttpClient client = new HttpClient();

            UriBuilder builder = new UriBuilder(uri);
            if (query != null && query.Count > 0)
            {
                builder.Query += string.Join("&", query.Select(x => x.Key + "=" + x.Value).ToArray());
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, builder.ToString());

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
                request.Headers.Add("Client-Id", token.ClientID);
            }

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
            return JsonConvert.DeserializeObject<TResp>(retContentStrTask.Result);
        }

        public static ResponseJObject GetJObject(string uri, Token token, Dictionary<string, string> query)
        {
            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (query != null && query.Count > 0)
                request.Content = new FormUrlEncodedContent(query);

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
                request.Headers.Add("Client-Id", token.ClientID);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Task<HttpResponseMessage> responseTask = client.SendAsync(request);
            responseTask.Wait(30 * 1000);
            HttpResponseMessage response = responseTask.Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ResponseJObject(response.StatusCode);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJObject(response.StatusCode, retContentStrTask.Result);
        }

        public static ResponseJArray GetJArray(string uri, Token token, Dictionary<string, string> query)
        {
            // TODO de-duplicate HttpRequest-related code
            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (query != null && query.Count > 0)
                request.Content = new FormUrlEncodedContent(query);

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
                request.Headers.Add("Client-Id", token.ClientID);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Task<HttpResponseMessage> responseTask = client.SendAsync(request);
            responseTask.Wait(30 * 1000);
            HttpResponseMessage response = responseTask.Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ResponseJArray(response.StatusCode);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJArray(response.StatusCode, retContentStrTask.Result);
        }
    }
}
