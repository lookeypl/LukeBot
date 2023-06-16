using LukeBot.Common;
using System;
using System.Threading;
using System.Collections.Generic;


namespace LukeBot.API
{
    public class AuthManager
    {
        private static readonly Lazy<AuthManager> mInstance =
            new Lazy<AuthManager>(() => new AuthManager());
        public static AuthManager Instance { get { return mInstance.Value; } }

        Dictionary<string, Token> mTokens;
        Mutex mMutex;

        private Token NewTokenForService(ServiceType service, string lbUser, string userId)
        {
            switch (service)
            {
            case ServiceType.Twitch: return new TwitchToken(AuthFlow.AuthorizationCode, lbUser, userId);
            case ServiceType.Spotify: return new SpotifyToken(AuthFlow.AuthorizationCode, lbUser, userId);
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private string FormTokenDictionaryKey(ServiceType service, string lbUser, string id)
        {
            return service.ToString() + "." + lbUser + id;
        }

        private AuthManager()
        {
            mTokens = new Dictionary<string, Token>();
            mMutex = new Mutex();
        }


        public Token GetToken(ServiceType service, string lbUser, string userId)
        {
            string tokenKey = FormTokenDictionaryKey(service, lbUser, userId);

            mMutex.WaitOne();

            Token ret;
            if (!mTokens.TryGetValue(tokenKey, out ret))
            {
                ret = NewTokenForService(service, lbUser, userId);
                mTokens[tokenKey] = ret;
            }

            mMutex.ReleaseMutex();
            return ret;
        }

        public void InvalidateToken(ServiceType service, string lbUser, string id)
        {
            string tokenKey = FormTokenDictionaryKey(service, lbUser, id);

            mMutex.WaitOne();

            Token t;
            if (mTokens.TryGetValue(tokenKey, out t))
            {
                t.Remove();
                mTokens.Remove(tokenKey);
            }

            mMutex.ReleaseMutex();
        }
    }
}
