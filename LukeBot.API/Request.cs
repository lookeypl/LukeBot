using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LukeBot.Common;


namespace LukeBot.API
{
    public class Request
    {
        private const int RESPONSE_WAIT_TIMEOUT = 30 * 1000; // ms

        private static HttpRequestMessage FormRequestMessage(HttpMethod method, string uri, Token token, Dictionary<string, string> uriQuery, Dictionary<string, string> contentQuery)
        {
            UriBuilder builder = new UriBuilder(uri);
            if (uriQuery != null && uriQuery.Count > 0)
            {
                builder.Query += string.Join("&", uriQuery.Select(x => x.Key + "=" + x.Value).ToArray());
            }

            HttpRequestMessage request = new HttpRequestMessage(method, builder.ToString());

            if (contentQuery != null && contentQuery.Count > 0)
                request.Content = new FormUrlEncodedContent(contentQuery);

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
                request.Headers.Add("Client-Id", token.ClientID);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }

        private static HttpResponseMessage Send(HttpMethod method, string uri, Token token, Dictionary<string, string> uriQuery, Dictionary<string, string> contentQuery)
        {
            if (token != null)
                token.EnsureValid();

            HttpClient client = new HttpClient();
            Task<HttpResponseMessage> responseTask = client.SendAsync(FormRequestMessage(method, uri, token, uriQuery, contentQuery));
            responseTask.Wait(RESPONSE_WAIT_TIMEOUT);
            HttpResponseMessage response = responseTask.Result;

            // refresh token and retry if we got unauthorized
            if (response.StatusCode == HttpStatusCode.Unauthorized && token != null)
            {
                token.Refresh();

                // recreate HttpClient to avoid re-send protection triggering
                client = new HttpClient();

                responseTask = client.SendAsync(FormRequestMessage(method, uri, token, uriQuery, contentQuery));
                responseTask.Wait(RESPONSE_WAIT_TIMEOUT);
                response = responseTask.Result;
            }

            return response;
        }

        public static TResp Get<TResp>(string uri,
                                       Token token = null,
                                       Dictionary<string, string> uriQuery = null,
                                       Dictionary<string, string> contentQuery = null)
                                       where TResp: Response, new()
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, contentQuery);
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

        public static ResponseJObject GetJObject(string uri,
                                                 Token token = null,
                                                 Dictionary<string, string> uriQuery = null,
                                                 Dictionary<string, string> contentQuery = null)
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, contentQuery);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ResponseJObject(response.StatusCode);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJObject(response.StatusCode, retContentStrTask.Result);
        }

        public static ResponseJArray GetJArray(string uri,
                                               Token token = null,
                                               Dictionary<string, string> uriQuery = null,
                                               Dictionary<string, string> contentQuery = null)
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, contentQuery);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ResponseJArray(response.StatusCode);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJArray(response.StatusCode, retContentStrTask.Result);
        }

        public static HttpResponseMessage Post(string uri,
                                               Token token = null,
                                               Dictionary<string, string> uriQuery = null)
        {
            return Send(HttpMethod.Post, uri, token, uriQuery, null);
        }
    }
}
