using System;
using LukeBot.Common;
using LukeBot.API;
using Widget = LukeBot.Widget;


namespace LukeBot.Twitch
{
    public class TwitchUserModule: IModule
    {
        private string mName;
        private Token mUserToken;
        private API.Twitch.GetUserResponse mUserData;
        private PubSub mPubSub;


        public TwitchUserModule(Token botToken, string channelName)
        {
            mName = channelName;
            mUserData = API.Twitch.GetUser(botToken, mName);

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, channelName);

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mPubSub = new PubSub(mUserToken);
            mPubSub.Listen(mUserData);
        }

        internal API.Twitch.GetUserResponse GetUserData()
        {
            return mUserData;
        }

        // IModule overrides

        public void Init()
        {
        }

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
    }
}
