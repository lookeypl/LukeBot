using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using LukeBot.Common;
using LukeBot.Config;


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
        private string mTokenPath = null;
        private AuthToken mToken = null;
        private Mutex mMutex = null;

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
            Loaded = true;
        }

        private void ExportToConfig()
        {
            if (Conf.Exists(mTokenPath))
                Conf.Remove(mTokenPath);

            // Auth tokens should always be created hidden
            Conf.Add(mTokenPath, Property.Create<AuthToken>(mToken, true));
            Conf.Save();
            Loaded = true;
        }

        public Token(string service, string lbUser, string userId, AuthFlow flow, string authURL, string refreshURL, string revokeURL, string callbackURL)
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

            mMutex = new Mutex();

            mTokenPath = Utils.FormConfName(
                Common.Constants.PROP_STORE_USER_DOMAIN, lbUser, service, Common.Constants.PROP_STORE_TOKEN_PROP
            );

            if (Conf.Exists(mTokenPath)) {
                Logger.Log().Debug("Found token at config {0}", mTokenPath);
                ImportFromConfig();
            }
        }

        ~Token()
        {
        }

        public string Get()
        {
            mMutex.WaitOne();

            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public string Request(string scope)
        {
            mMutex.WaitOne();

            mToken = mFlow.Request(scope);
            ExportToConfig();

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public string Refresh()
        {
            mMutex.WaitOne();

            if (mToken == null)
                throw new InvalidTokenException("Token has not been acquired yet");

            AuthToken oldToken = mToken;
            mToken = mFlow.Refresh(mToken);

            // preserve refresh token - some services (ex. Spotify) don't provide it in refresh response
            if (mToken.refresh_token == null)
                mToken.refresh_token = oldToken.refresh_token;

            ExportToConfig();

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public void Remove()
        {
            mMutex.WaitOne();

            if (Loaded) {
                Conf.Remove(mTokenPath);
                mFlow.Revoke(mToken);
                mToken = null;
                Loaded = false;
            }

            mMutex.ReleaseMutex();
        }
    }
}
