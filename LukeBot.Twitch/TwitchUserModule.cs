using System;
using System.Collections.Generic;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Twitch.EventSub;
using Widget = LukeBot.Widget;


namespace LukeBot.Twitch
{
    public class TwitchUserModule: IUserModule
    {
        private string mLBUser;
        private string mChannelName;
        private Token mUserToken;
        private API.Twitch.GetUserData mUserData;
        private EventSubClient mEventSub;


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

            //mEventSub = new();
            //mEventSub.Connect(mUserToken, mUserData.id);

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            //mEventSub.Subscribe(events);
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
            if (mEventSub != null) mEventSub.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mEventSub != null) mEventSub.WaitForShutdown();
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Twitch;
        }
    }
}
