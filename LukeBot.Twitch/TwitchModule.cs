using System;
using System.Net;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        private Token mToken;
        private Token mUserToken;
        private TwitchIRC mIRC;
        private PubSub mPubSub;
        private API.GetUserResponse mBotData;
        private API.GetUserResponse mUserData;
        private ChatWidget mWidget;

        bool IsLoginSuccessful()
        {
            mBotData = API.GetUser(mToken);
            if (mBotData.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Twitch login successful");
                return true;
            }
            else if (mBotData.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Twitch - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Twitch: " + mBotData.code.ToString());
        }

        public TwitchModule()
        {
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);

            string tokenScope = "chat:read chat:edit user:read:email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lukebot");

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!IsLoginSuccessful())
            {
                if (tokenFromFile)
                {
                    mToken.Refresh();
                    if (!IsLoginSuccessful())
                    {
                        mToken.Remove();
                        throw new InvalidOperationException(
                            "Failed to refresh Twitch OAuth Token. Token has been removed, restart to re-login and request a fresh OAuth token"
                        );
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Twitch");
            }

            mIRC = new TwitchIRC(mToken);
            mWidget = new ChatWidget(mIRC);
        }

        // TEMPORARY
        public void JoinChannel(string channel)
        {
            Logger.Log().Debug("Joining channel {0}", channel);
            mUserData = API.GetUser(mToken, channel);

            mIRC.JoinChannel(mUserData);

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lookey");

            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            mPubSub = new PubSub(mUserToken);
            mPubSub.Listen(mUserData);
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
            mIRC.RequestShutdown();
            mPubSub.RequestShutdown();
            mWidget.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            mIRC.WaitForShutdown();
            mPubSub.WaitForShutdown();
            mWidget.WaitForShutdown();
        }
    }
}
