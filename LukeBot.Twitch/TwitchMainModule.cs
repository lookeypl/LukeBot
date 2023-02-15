using System;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Config;

namespace LukeBot.Twitch
{
    public class TwitchMainModule
    {
        private string mBotLogin;
        private Token mToken;
        private TwitchIRC mIRC;
        private API.Twitch.GetUserResponse mBotData;
        private Dictionary<string, TwitchUserModule> mUsers;

        public TwitchMainModule()
        {
            Comms.Communication.Register(Constants.SERVICE_NAME);

            mBotLogin = Conf.Get<string>("twitch.login");
            if (mBotLogin == LukeBot.Common.Constants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }

            string tokenScope = "chat:read chat:edit user:read:email"; // TODO should also be from Config...
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.Twitch.GetUser(mToken);
            mIRC = new TwitchIRC(mBotLogin, mToken);
            mUsers = new Dictionary<string, TwitchUserModule>();
        }

        public void JoinChannel(string channel)
        {
            Logger.Log().Debug("Joining channel {0}", channel);

            if (mUsers.ContainsKey(channel))
            {
                throw new ChannelAlreadyJoinedException("Cannot join channel {0} - already joined", channel);
            }

            TwitchUserModule user = new TwitchUserModule(mToken, channel);

            mIRC.JoinChannel(user.GetUserData());

            mUsers.Add(channel, user);

            Logger.Log().Secure("Joined channel twitch ID: {0}", user.GetUserData().data[0].id);
        }

        public void AddCommandToChannel(string channel, string commandName, Command.ICommand command)
        {
            mIRC.AddCommandToChannel(channel, commandName, command);
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.RequestShutdown();

            if (mIRC != null) mIRC.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.WaitForShutdown();

            if (mIRC != null) mIRC.WaitForShutdown();
        }
    }
}