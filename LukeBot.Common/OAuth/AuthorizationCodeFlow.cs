using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using LukeBot.Common.Exception;

namespace LukeBot.Common.OAuth
{
    class AuthorizationCodeFlow: Flow
    {
        private readonly HttpClient mClient = new HttpClient();
        private string mCallbackURL = null;

        public override AuthTokenResponse Request(string scope)
        {
            //
            // Step 1: Acquire user token
            //
            Logger.Info("Requesting OAuth user token...");
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
            Logger.Debug("Login window with URI " + URL);

            Logger.Debug("Notifying comms manager");
            PromiseData userResponseBase = new UserTokenResponse();
            IntermediaryPromise userPromise = CommunicationManager.Instance.GetIntermediary(mService).Expect(state, ref userResponseBase);

            Logger.Debug("Opening browser window with query {0}", URL);
            Utils.StartBrowser(URL);

            // wait for 5 minutes
            if (!userPromise.Wait(5 * 60 * 1000))
                throw new PromiseRejectedException(String.Format("Promise for service {0} rejected/timed out", mService));

            Logger.Debug("Promise {0} for service {1} fulfilled", state, mService);

            // TODO we probably should hold on to this token? Check
            UserTokenResponse userResponse = (UserTokenResponse)userResponseBase;
            Logger.Debug("User token from service {0}:", mService);
            Logger.Debug("  Code: {0}", userResponse.code);
            Logger.Debug("  Scope: ");
            foreach (var s in userResponse.scope)
            {
                Logger.Debug("    -> {0}", s);
            }
            Logger.Debug("  State: {0}", userResponse.state);


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
            Logger.Debug("Sending POST with content " + contentStrTask.Result);

            Task<HttpResponseMessage> retMessageTask = mClient.PostAsync(mTokenURL, content);
            retMessageTask.Wait(30 * 1000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            Logger.Debug("Response status code is " + retMessage.StatusCode);
            retMessage.EnsureSuccessStatusCode();

            HttpContent retContent = retMessage.Content;
            Logger.Debug("Received content type " + retContent.Headers.ContentType);

            Task<string> retContentStrTask = retContent.ReadAsStringAsync();
            retContentStrTask.Wait();
            string retContentStr = retContentStrTask.Result;

            Logger.Debug("Received content: {0}", retContentStr);

            AuthTokenResponse authResponse = JsonSerializer.Deserialize<AuthTokenResponse>(retContentStr);
            Logger.Debug("Response from OAuth service {0}:", mService);
            Logger.Debug("  Access token: {0}", authResponse.access_token);
            Logger.Debug("  Refresh token: {0}", authResponse.refresh_token);
            Logger.Debug("  Expires in: {0}", authResponse.expires_in);
            Logger.Debug("  Scope: ");
            foreach (var s in authResponse.scope)
            {
                Logger.Debug("    -> {0}", s);
            }
            Logger.Debug("  Token type: {0}", authResponse.token_type);

            return authResponse;
        }

        public override string Refresh(string token)
        {
            throw new NotImplementedException();
        }

        public override void Revoke(string token)
        {
            throw new NotImplementedException();
        }

        public AuthorizationCodeFlow(string service, string idPath, string secretPath,
                                     string authURL, string refreshURL, string revokeURL, string callbackURL)
            : base(service, idPath, secretPath, authURL, refreshURL, revokeURL)
        {
            mCallbackURL = callbackURL;
        }
    }
}
