using LukeBot.Common;
using System;
using System.Threading;

namespace LukeBot.Auth
{
    public class AuthManager
    {
        private static readonly Lazy<AuthManager> mInstance =
            new Lazy<AuthManager>(() => new AuthManager());
        public static AuthManager Instance { get { return mInstance.Value; } }

        Token[] mTokens;
        Mutex mMutex;

        private Token NewTokenForService(ServiceType service)
        {
            switch (service)
            {
            case ServiceType.Twitch: return new TwitchToken(AuthFlow.AuthorizationCode);
            case ServiceType.Spotify: return new SpotifyToken(AuthFlow.AuthorizationCode);
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private AuthManager()
        {
            mTokens = new Token[(int)ServiceType.Count];
            for (int i = 0; i < mTokens.Length; ++i)
                mTokens[i] = null;

            mMutex = new Mutex();
        }


        // Get a token from
        public Token GetToken(ServiceType service)
        {
            mMutex.WaitOne();

            Token ret = mTokens[(int)service];
            if (ret == null)
            {
                ret = NewTokenForService(service);
                mTokens[(int)service] = ret;
            }

            mMutex.ReleaseMutex();
            return ret;
        }

        public void InvalidateToken(ServiceType service)
        {
            mMutex.WaitOne();

            Token t = mTokens[(int)service];
            t.Remove();
            mTokens[(int)service] = null;

            mMutex.ReleaseMutex();
        }
    }
}
