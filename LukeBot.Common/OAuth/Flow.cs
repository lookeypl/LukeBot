using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LukeBot.Common.OAuth
{
    abstract class Flow
    {
        protected string mService;
        protected string mClientID;
        protected string mClientSecret;
        protected string mAuthURL;
        protected string mTokenURL;
        protected string mRevokeURL;

        private string ReadFromFile(string path)
        {
            StreamReader fileStream = File.OpenText(path);
            return fileStream.ReadLine();
        }

        protected Flow(string service, string idPath, string secretPath, string authURL, string tokenURL, string revokeURL)
        {
            mService = service;
            mClientID = ReadFromFile(idPath);
            mClientSecret = ReadFromFile(secretPath);
            mAuthURL = authURL;
            mTokenURL = tokenURL;
            mRevokeURL = revokeURL;
        }

        public abstract AuthTokenResponse Request(string scope);
        public abstract string Refresh(string token);
        public abstract void Revoke(string token);
    }
}
