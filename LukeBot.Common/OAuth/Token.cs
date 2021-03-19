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
        private AuthToken mToken;

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
            return mToken.access_token;
        }

        public string Refresh()
        {
            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            mToken = mFlow.Refresh(mToken);
            return mToken.access_token;
        }

        public void Revoke()
        {
            mFlow.Revoke(mToken);
            mToken = null;
        }

        public void ImportFromFile(string path)
        {
            StreamReader fileStream = File.OpenText(path);
            mToken = JsonSerializer.Deserialize<AuthToken>(fileStream.ReadToEnd());
            fileStream.Close();
        }

        public void ExportToFile(string path)
        {
            if (mToken == null)
                throw new InvalidTokenException("Token is not acquired");

            FileStream file = File.OpenWrite(path);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(JsonSerializer.Serialize(mToken));
            writer.Close();
            file.Close();
        }
    }
}
