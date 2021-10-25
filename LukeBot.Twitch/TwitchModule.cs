using System;
using System.Net;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        private Token mToken;
        private TwitchIRC mIRC;
        private ChatWidget mWidget;

        bool IsLoginSuccessful()
        {
            API.GetUserResponse testResponse = API.GetUser(mToken);
            if (testResponse.code == HttpStatusCode.OK)
            {
                Logger.Debug("Twitch login successful");
                return true;
            }
            else if (testResponse.code == HttpStatusCode.Unauthorized)
            {
                Logger.Error("Failed to login to Twitch - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Twitch: " + testResponse.code.ToString());
        }

        public TwitchModule()
        {
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);

            string tokenScope = "chat:read chat:edit user:read:email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch);

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
            Logger.Debug("Joining channel {0}", channel);
            mIRC.JoinChannel(channel);
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
            mWidget.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            mIRC.WaitForShutdown();
            mWidget.WaitForShutdown();
        }
    }
}
