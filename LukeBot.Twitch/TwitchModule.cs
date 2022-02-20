using System;
using System.Net;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Core;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        private Token mToken;
        private Token mUserToken;
        private TwitchIRC mIRC;
        private PubSub mPubSub;
        private API.Twitch.GetUserResponse mBotData;
        private API.Twitch.GetUserResponse mUserData;


        private bool IsLoginSuccessful(Token token)
        {
            API.Twitch.GetUserResponse data = API.Twitch.GetUser(token);
            if (data.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Twitch login successful");
                return true;
            }
            else if (data.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Twitch - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Twitch: " + mBotData.code.ToString());
        }

        public TwitchModule()
        {
            Systems.Communication.Register(Constants.SERVICE_NAME);

            string tokenScope = "chat:read chat:edit user:read:email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lukebot");

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!IsLoginSuccessful(mToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.Twitch.GetUser(mToken);

            mIRC = new TwitchIRC(mToken);
            //mChatWidget = new ChatWidget();
        }

        // TEMPORARY
        public void JoinChannel(string channel)
        {
            Logger.Log().Debug("Joining channel {0}", channel);
            mUserData = API.Twitch.GetUser(mToken, channel);

            mIRC.JoinChannel(mUserData);

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lookey");

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mPubSub = new PubSub(mUserToken);
            mPubSub.Listen(mUserData);

            //mAlertsWidget = new AlertsWidget();

            Logger.Log().Secure("Joined channel twitch ID: {0}", mUserData.data[0].id);
        }

        // TEMPORARY
        public void AddCommandToChannel(string channel, string commandName, Command.ICommand command)
        {
            mIRC.AddCommandToChannel(channel, commandName, command);
        }

        // TEMPORARY
        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }


        // IModule overrides

        public void Init()
        {
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            if (mIRC != null) mIRC.RequestShutdown();
            if (mPubSub != null) mPubSub.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mIRC != null) mIRC.WaitForShutdown();
            if (mPubSub != null) mPubSub.WaitForShutdown();
        }
    }
}
