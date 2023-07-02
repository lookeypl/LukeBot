using System;
using System.Threading;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;


namespace LukeBot.API
{
    public class AuthManager
    {
        private static readonly Lazy<AuthManager> mInstance =
            new Lazy<AuthManager>(() => new AuthManager());
        public static AuthManager Instance { get { return mInstance.Value; } }

        Dictionary<Path, Token> mTokens = new();
        Mutex mMutex = new();

        private Token NewTokenForService(ServiceType service, string lbUser)
        {
            switch (service)
            {
            case ServiceType.Twitch: return new TwitchToken(AuthFlow.AuthorizationCode, lbUser);
            case ServiceType.Spotify: return new SpotifyToken(AuthFlow.AuthorizationCode, lbUser);
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private Path FormTokenDictionaryKey(ServiceType service, string lbUser)
        {
            return Path.Start()
                .Push(Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(service.ToString().ToLower())
                .Push(Common.Constants.PROP_STORE_TOKEN_PROP);
        }

        private AuthManager()
        {
        }


        public Token GetToken(ServiceType service, string lbUser)
        {
            Path tokenKey = FormTokenDictionaryKey(service, lbUser);

            mMutex.WaitOne();

            Token ret;
            if (!mTokens.TryGetValue(tokenKey, out ret))
            {
                ret = NewTokenForService(service, lbUser);
                mTokens[tokenKey] = ret;
            }

            mMutex.ReleaseMutex();
            return ret;
        }

        public void InvalidateToken(ServiceType service, string lbUser)
        {
            Path tokenKey = FormTokenDictionaryKey(service, lbUser);

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
