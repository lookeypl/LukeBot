using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Core;


namespace LukeBot.API
{
    class AuthorizationCodeFlow: Flow
    {
        private readonly HttpClient mClient = new HttpClient();
        private string mCallbackURL = null;

        public override AuthToken Request(string scope)
        {
            //
            // Step 1: Acquire user token
            //
            Logger.Log().Info("Requesting OAuth user token...");
            Dictionary<string, string> query = new Dictionary<string, string>();

            // TODO this MUST be random!
            string state = "CHANGEMETOSOMETHINGRANDOM";

            query.Add("client_id", mClientID);
            query.Add("redirect_uri", mCallbackURL);
            query.Add("response_type", "code");
            query.Add("scope", scope);
            query.Add("state", state);

            FormUrlEncodedContent content = new FormUrlEncodedContent(query);

            Task<string> contentStrTask = content.ReadAsStringAsync();
            contentStrTask.Wait();

            string URL = mAuthURL + '?' + contentStrTask.Result;
            Logger.Log().Debug("Login window with URI " + URL);

            Logger.Log().Debug("Notifying comms manager");
            PromiseData userResponseBase = new UserToken();
            IntermediaryPromise userPromise = Systems.Communication.GetIntermediary(mService).Expect(state, ref userResponseBase);

            Logger.Log().Debug("Opening browser window with query {0}", URL);
            Utils.StartBrowser(URL);

            // wait for 5 minutes
            if (!userPromise.Wait(5 * 60 * 1000))
                throw new PromiseRejectedException(String.Format("Promise for service {0} rejected/timed out", mService));

            Logger.Log().Debug("Promise {0} for service {1} fulfilled", state, mService);

            // TODO we probably should hold on to this token? Check if that's the case
            UserToken userResponse = (UserToken)userResponseBase;
            Logger.Log().Debug("User token from service {0}:", mService);
            Logger.Log().Secure("  Code: {0}", userResponse.code);
            // TODO commented out, since services treat "Scope" differently:
            //  - Twitch - should be List<string>
            //  - Spotify - should be string
            // In the future it would be nice to cross-check if we got scopes we wanted
            /*Logger.Log().Debug("  Scope: ");
            foreach (var s in userResponse.scope)
            {
                Logger.Log().Debug("    -> {0}", s);
            }*/
            Logger.Log().Debug("  State: {0}", userResponse.state);


            //
            // Step 2: Get OAuth access token
            //
            query.Clear();

            query.Add("client_id", mClientID);
            query.Add("client_secret", mClientSecret);
            query.Add("code", userResponse.code);
            query.Add("grant_type", "authorization_code");
            query.Add("redirect_uri", mCallbackURL);

            content = new FormUrlEncodedContent(query);

            contentStrTask = content.ReadAsStringAsync();
            contentStrTask.Wait();
            Logger.Log().Debug("Sending POST request");
            Logger.Log().Secure(" -> Content: {0}", contentStrTask.Result);

            Task<HttpResponseMessage> retMessageTask = mClient.PostAsync(mTokenURL, content);
            retMessageTask.Wait(30 * 1000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            Logger.Log().Debug("Response status code is " + retMessage.StatusCode);
            retMessage.EnsureSuccessStatusCode();

            HttpContent retContent = retMessage.Content;
            Logger.Log().Debug("Received content type " + retContent.Headers.ContentType);

            Task<string> retContentStrTask = retContent.ReadAsStringAsync();
            retContentStrTask.Wait();
            string retContentStr = retContentStrTask.Result;

            Logger.Log().Secure("Returned content {0}", retContentStr);

            AuthToken authResponse = JsonSerializer.Deserialize<AuthToken>(retContentStr);
            Logger.Log().Debug("Response from OAuth service {0}:", mService);
            Logger.Log().Secure("  Access token: {0}", authResponse.access_token);
            Logger.Log().Secure("  Refresh token: {0}", authResponse.refresh_token);
            Logger.Log().Debug("  Expires in: {0}", authResponse.expires_in);
            /*Logger.Log().Debug("  Scope: ");
            foreach (var s in authResponse.scope)
            {
                Logger.Log().Debug("    -> {0}", s);
            }*/
            Logger.Log().Debug("  Token type: {0}", authResponse.token_type);

            return authResponse;
        }

        public override AuthToken Refresh(AuthToken token)
        {
            Logger.Log().Debug("Refreshing OAuth token...");

            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Clear();

            query.Add("grant_type", "refresh_token");
            query.Add("refresh_token", token.refresh_token);
            query.Add("client_id", mClientID);
            query.Add("client_secret", mClientSecret);

            FormUrlEncodedContent content = new FormUrlEncodedContent(query);

            Task<string> contentStrTask = content.ReadAsStringAsync();
            contentStrTask.Wait();
            Logger.Log().Debug("Sending POST request");
            Logger.Log().Secure(" -> Content: {0}", contentStrTask.Result);

            Task<HttpResponseMessage> retMessageTask = mClient.PostAsync(mTokenURL, content);
            retMessageTask.Wait(30 * 1000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            Logger.Log().Debug("Response status code is " + retMessage.StatusCode);
            retMessage.EnsureSuccessStatusCode();

            HttpContent retContent = retMessage.Content;
            Logger.Log().Debug("Received content type " + retContent.Headers.ContentType);

            Task<string> retContentStrTask = retContent.ReadAsStringAsync();
            retContentStrTask.Wait();
            string retContentStr = retContentStrTask.Result;

            AuthToken refreshResponse = JsonSerializer.Deserialize<AuthToken>(retContentStr);
            Logger.Log().Debug("Response from OAuth service {0}:", mService);
            Logger.Log().Secure("  Access token: {0}", refreshResponse.access_token);
            Logger.Log().Secure("  Refresh token: {0}", refreshResponse.refresh_token);
            Logger.Log().Debug("  Expires in: {0}", refreshResponse.expires_in);
            Logger.Log().Debug("  Token type: {0}", refreshResponse.token_type);
            /*Logger.Log().Debug("  Scope: ");
            foreach (var s in refreshResponse.scope)
            {
                Logger.Log().Debug("    -> {0}", s);
            }*/

            return refreshResponse;
        }

        public override void Revoke(AuthToken token)
        {
            Logger.Log().Info("Revoking previously acquired OAuth token...");
            Dictionary<string, string> query = new Dictionary<string, string>();

            // TODO client_id and client_secret should come from PropertyStore
            query.Add("client_id", mClientID);
            query.Add("token", token.access_token);

            FormUrlEncodedContent content = new FormUrlEncodedContent(query);
            Task<HttpResponseMessage> retMessageTask = mClient.PostAsync(mRevokeURL, content);
            retMessageTask.Wait(30 * 1000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            if (!retMessage.IsSuccessStatusCode)
            {
                Logger.Log().Error("Failed to revoke OAuth token");
            }
            else
            {
                Logger.Log().Info("OAuth Token revoked successfully");
            }
        }

        public AuthorizationCodeFlow(string service, string idPath, string secretPath,
                                     string authURL, string refreshURL, string revokeURL, string callbackURL)
            : base(service, idPath, secretPath, authURL, refreshURL, revokeURL)
        {
            mCallbackURL = callbackURL;
        }
    }
}
