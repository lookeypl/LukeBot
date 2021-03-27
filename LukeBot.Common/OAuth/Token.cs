using System;
using System.IO;
using System.Text.Json;

namespace LukeBot.Common.OAuth
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
            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            return mToken.access_token;
        }

        public string Request(string scope)
        {
            mToken = mFlow.Request(scope);
            ExportToFile();
            return mToken.access_token;
        }

        public string Refresh()
        {
            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            mToken = mFlow.Refresh(mToken);
            ExportToFile();
            return mToken.access_token;
        }

        public void Remove()
        {
            if (Loaded) {
                File.Delete(mTokenPath);
                mFlow.Revoke(mToken);
                mToken = null;
            }
        }
    }
}
