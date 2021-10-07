using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using LukeBot.Common;


namespace LukeBot.Auth
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


        private void ImportFromFile()
        {
            StreamReader fileStream = File.OpenText(mTokenPath);
            mToken = JsonSerializer.Deserialize<AuthToken>(fileStream.ReadToEnd());
            fileStream.Close();
            Loaded = true;
        }

        private void ExportToFile()
        {
            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            if (File.Exists(mTokenPath))
                File.Delete(mTokenPath);

            FileStream file = File.OpenWrite(mTokenPath);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(JsonSerializer.Serialize(mToken));
            writer.Close();
            file.Close();
            Loaded = true;
        }

        public Token(string service, AuthFlow flow, string authURL, string refreshURL, string revokeURL, string callbackURL)
        {
            string idPath = "Data/" + service + ".client_id.lukebot";
            string secretPath = "Data/" + service + ".client_secret.lukebot";

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

            mTokenPath = "Data/" + service + ".token.lukebot";
            mMutex = new Mutex();

            if (FileUtils.Exists(mTokenPath)) {
                Logger.Debug("Found token {0}, importing", mTokenPath);
                ImportFromFile();
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
            ExportToFile();

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public string Refresh()
        {
            mMutex.WaitOne();

            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            AuthToken oldToken = mToken;
            mToken = mFlow.Refresh(mToken);

            // preserve refresh token - some services (ex. Spotify) don't provide it in refresh response
            if (mToken.refresh_token == null)
                mToken.refresh_token = oldToken.refresh_token;

            ExportToFile();

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public void Remove()
        {
            mMutex.WaitOne();

            if (Loaded) {
                File.Delete(mTokenPath);
                mFlow.Revoke(mToken);
                mToken = null;
                Loaded = false;
            }

            mMutex.ReleaseMutex();
        }
    }
}
