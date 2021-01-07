using System;
using System.Collections.Generic;

namespace LukeBot.Common.OAuth
{
    public enum AuthFlow
    {
        AuthorizationCode,
        ClientCredentials
    }

    public class Token
    {
        // TODO OAuth token validator thread
        private Flow mFlow = null;
        private Dictionary<string, AuthTokenResponse> mTokens = new Dictionary<string, AuthTokenResponse>();

        public Token(string service, AuthFlow flow, string authURL, string refreshURL, string revokeURL, string callbackURL)
        {
            string idPath = "Data/oauth_client_id.lukebot";
            string secretPath = "Data/oauth_client_secret.lukebot";

            switch (flow)
            {
            case AuthFlow.AuthorizationCode:
                mFlow = new AuthorizationCodeFlow(service, idPath, secretPath, authURL, refreshURL, revokeURL, callbackURL);
                break;
            case AuthFlow.ClientCredentials:
                mFlow = new ClientCredentialsFlow(service, idPath, secretPath, authURL, refreshURL, revokeURL);
                break;
            default:
                throw new ArgumentOutOfRangeException("Invalid AuthFlow mode: {0}" + flow.ToString());
            }
        }

        ~Token()
        {
        }

        public string Get(string scope)
        {
            if (!mTokens.ContainsKey(scope))
                mTokens.Add(scope, mFlow.Request(scope));

            return mTokens[scope].access_token;
        }
    }
}
