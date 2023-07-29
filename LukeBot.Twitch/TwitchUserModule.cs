using System;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Module;
using Widget = LukeBot.Widget;


namespace LukeBot.Twitch
{
    public class TwitchUserModule: IUserModule
    {
        private string mLBUser;
        private string mChannelName;
        private Token mUserToken;
        private API.Twitch.GetUserData mUserData;
        private PubSub mPubSub;


        public TwitchUserModule(string lbUser, Token botToken, string channelName)
        {
            mLBUser = lbUser;
            mChannelName = channelName;
            API.Twitch.GetUserResponse resp = API.Twitch.GetUser(botToken, mChannelName);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Error("Failed to fetch user data from Twitch - received error code {0}", resp.code.ToString());
                throw new APIResponseErrorException(resp.code);
            }
            mUserData = resp.data[0];

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, lbUser);

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mPubSub = new PubSub(lbUser, mUserToken, mUserData);
            mPubSub.Connect(new Uri("wss://pubsub-edge.twitch.tv"));
        }

        internal API.Twitch.GetUserData GetUserData()
        {
            return mUserData;
        }

        internal string GetLBUser()
        {
            return mLBUser;
        }

        internal string GetChannelName()
        {
            return mChannelName;
        }

        // IUserModule overrides

        public void Run()
        {
        }

        public void RequestShutdown()
        {
            if (mPubSub != null) mPubSub.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mPubSub != null) mPubSub.WaitForShutdown();
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Twitch;
        }
    }
}
