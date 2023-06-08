using System;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Module;
using Widget = LukeBot.Widget;


namespace LukeBot.Twitch
{
    public class TwitchUserModule: IUserModule
    {
        private string mLBUser;
        private string mChannelName;
        private Token mUserToken;
        private API.Twitch.GetUserResponse mUserData;
        private PubSub mPubSub;


        public TwitchUserModule(string lbUser, Token botToken, string channelName)
        {
            mLBUser = lbUser;
            mChannelName = channelName;
            mUserData = API.Twitch.GetUser(botToken, mChannelName);

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, channelName);

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mPubSub = new PubSub(mUserToken, mUserData);
            mPubSub.Connect();
        }

        internal API.Twitch.GetUserResponse GetUserData()
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
