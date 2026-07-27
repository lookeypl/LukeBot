using System;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Config;
using LukeBot.Logging;


namespace LukeBot.API
{
    public enum AuthFlow
    {
        AuthorizationCode,
        ClientCredentials
    }

    public class Token
    {
        private Flow mFlow = null;
        private Config.Path mTokenPath = null;
        private AuthToken mToken = null;
        private bool mScopeUpdated = true;
        private List<string> mScope = new();
        private object mTokenLock = new();
        private string mLBUser = null;

        // Check if token is valid. This can be false when Token is either
        // not loaded, or loaded but past its expiration period.
        private bool IsValid
        {
            get
            {
                if (!Loaded)
                    return false;

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return now < mToken.acquiredTimestamp + mToken.expires_in;
            }
        }

        // Check if token is loaded. This ensures it has been loaded from
        // ex. Config, but it still might be an old (expired) token.
        public bool Loaded { get; private set; }
        public string ClientID
        {
            get
            {
                return mFlow.ClientID;
            }
        }

        private void ImportFromConfig()
        {
            mToken = Conf.Get<AuthToken>(mTokenPath);
        }

        private void ExportToConfig()
        {
            if (Conf.Exists(mTokenPath))
                Conf.Remove(mTokenPath);

            // Auth tokens should always be created hidden
            Conf.Add(mTokenPath, Property.Create<AuthToken>(mToken, true));
            Conf.Save();
        }

        private bool IsScopeMatching()
        {
            // cross-checks currently set mScope with AuthToken's scope
            // If they mismatch, returns false. This is used to notify that we have a new Scope
            // requirement, which means the Token cannot just be refreshed but must be re-requested.
            if (mToken == null || mToken.scope == null) return false;
            if (mScope.Count != mToken.scope.Count) return false;

            foreach (string newScope in mScope)
            {
                bool found = false;
                foreach (string oldScope in mToken.scope)
                {
                    if (oldScope == newScope)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public Token(string service, string lbUser, AuthFlow flow, string authURL, string refreshURL, string revokeURL, string callbackURL)
        {
            switch (flow)
            {
            case AuthFlow.AuthorizationCode:
                mFlow = new AuthorizationCodeFlow(service, authURL, refreshURL, revokeURL, callbackURL);
                break;
            case AuthFlow.ClientCredentials:
                mFlow = new ClientCredentialsFlow(service, authURL, refreshURL, revokeURL);
                break;
            default:
                throw new ArgumentOutOfRangeException("Invalid AuthFlow mode: {0}" + flow.ToString());
            }

            mTokenPath = Config.Path.Start()
                .Push(Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(service)
                .Push(Common.Constants.PROP_STORE_TOKEN_PROP);

            Loaded = false;
            if (Conf.Exists(mTokenPath)) {
                Logger.Log().Debug("Found token at config {0}", mTokenPath);
                ImportFromConfig();
                Loaded = true;
            }

            mLBUser = lbUser;
        }

        ~Token()
        {
        }

        public string Get()
        {
            lock(mTokenLock)
            {
                if (mToken == null)
                {
                    throw new InvalidTokenException("Token is not acquired");
                }

                return mToken.access_token;
            }
        }

        public string Request()
        {
            lock (mTokenLock)
            {
                // re-check validity, in case other thread already requested a Token for us
                if (IsValid)
                {
                    return mToken.access_token;
                }

                mToken = mFlow.Request(mLBUser, mScope);
                ExportToConfig();
                Loaded = true;

                return mToken.access_token;
            }
        }

        public void SetScope(List<string> scope)
        {
            mScope = scope;
            mScopeUpdated = true;
        }

        public void EnsureValid()
        {
            bool needsRequest = false;
            lock (mTokenLock)
            {
                if (mScopeUpdated)
                {
                    needsRequest = !IsScopeMatching();
                    mScopeUpdated = false;
                }
            }

            if (needsRequest)
            {
                mToken.acquiredTimestamp = 0; // invalidates current token to let Request() recreate it
                Request();
            }

            if (!IsValid)
                Refresh();
        }

        public string Refresh()
        {
            lock (mTokenLock)
            {
                if (mToken == null)
                {
                    throw new InvalidTokenException("Token has not been acquired yet");
                }

                // Forces refresh by resetting the expiration timestamp
                mToken.expires_in = 0;

                // re-check validity, in case other thread already refreshed the Token for us
                if (IsValid)
                {
                    return mToken.access_token;
                }

                AuthToken oldToken = mToken;
                mToken = mFlow.Refresh(mToken);

                // preserve refresh token - some services (ex. Spotify) don't provide it in refresh response
                if (mToken.refresh_token == null || mToken.refresh_token.Length == 0)
                {
                    mToken.refresh_token = oldToken.refresh_token;
                }

                ExportToConfig();
                Loaded = true;
                return mToken.access_token;
            }
        }

        public void Remove()
        {
            lock (mTokenLock)
            {
                Conf.Remove(mTokenPath);

                if (Loaded) {
                    mFlow.Revoke(mToken);
                    mToken = null;
                    Loaded = false;
                }
            }
        }
    }
}
