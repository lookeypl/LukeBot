using System;
using System.Collections.Generic;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Twitch;
using LukeBot.User;

using CommonConstants = LukeBot.Common.Constants;
using CommonUtils = LukeBot.Common.Utils;


namespace LukeBot.Twitch.Impl
{
    public class TwitchService: ITwitchService, IUserModuleFactory
    {
        private string mBotLogin;
        private Token mBotToken;
        private TwitchIRC mIRC;
        private BadgeCollection mGlobalBadges;
        private Dictionary<string, TwitchUserModule> mUserModules = new();
        private List<string> mJoinedTwitchChannels = new();


        // Config interactions //

        private void LoadUserModulesFromConfig()
        {
            string[] users = CommonUtils.GetUserModulesFromConfig(CommonConstants.TWITCH_SERVICE_NAME);

            foreach (string u in users)
            {
                try
                {
                    CreateModule(ServiceUtils.GetUserService().GetUser(u));
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to initialize Twitch user module for user {0}: {1}", u, e.Message);
                    Logger.Log().Error("Twitch user module for user {0} will be skipped on this load.", u);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }
        }


        // Private helpers //

        private TwitchUserModule JoinChannel(IUserContext lbUser)
        {
            string channel = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser.GetUsername())
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );

            if (mUserModules.ContainsKey(lbUser.GetUsername()) ||
                mJoinedTwitchChannels.Exists((ch) => ch == channel))
            {
                throw new ChannelAlreadyJoinedException(lbUser.GetUsername());
            }

            Logger.Log().Debug("Joining {0} channel for user {1}", channel, lbUser.GetUsername());

            TwitchUserModule module = null;

            try
            {
                module = new TwitchUserModule(lbUser, mBotToken, mIRC, mGlobalBadges);
            }
            catch (System.Exception)
            {
                // get rid of our just created user module, as we might've already started something
                if (module != null)
                {
                    module.RequestShutdown();
                    module.WaitForShutdown();
                    module = null;
                }

                throw;
            }

            mJoinedTwitchChannels.Add(channel);
            Logger.Log().Secure("Joined Twitch channel {0} ({1})", module.GetChannelIdentity().Username, module.GetChannelIdentity().ID);
            return module;
        }

        private void PartChannel(IUserContext lbUser)
        {
            if (mUserModules.TryGetValue(lbUser.GetUsername(), out TwitchUserModule module))
            {
                Logger.Log().Debug("Parting channel {0} for user {1}", module.GetChannelName(), lbUser);

                mJoinedTwitchChannels.Remove(module.GetChannelName());

                try
                {
                    module.Dispose();
                }
                finally
                {
                    mUserModules.Remove(lbUser.GetUsername());
                }

                Logger.Log().Secure("Parted Twitch channel {0} ({1})", module.GetChannelIdentity().Username, module.GetChannelIdentity().ID);
            }
        }


        // IUserModuleFactory //

        public IUserModule CreateModule(IUserContext user)
        {
            TwitchUserModule module = JoinChannel(user);
            module.Run();

            mUserModules.Add(user.GetUsername(), module);
            user.AttachModule(module);
            CommonUtils.AddUserModuleToConfig(CommonConstants.TWITCH_SERVICE_NAME, user.GetUsername());

            return module;
        }

        public IUserModule GetModule(IUserContext user)
        {
            return mUserModules[user.GetUsername()];
        }

        public void DestroyModule(IUserContext user)
        {
            if (mUserModules.TryGetValue(user.GetUsername(), out TwitchUserModule module))
            {
                user.DetachModule(module);
                mUserModules.Remove(user.GetUsername());

                module.RequestShutdown();
                module.WaitForShutdown();

                CommonUtils.RemoveUserModuleFromConfig(CommonConstants.TWITCH_SERVICE_NAME, user.GetUsername());
            }
        }


        private TwitchService()
        {
            ServiceUtils.GetIntermediaryService().Register(CommonConstants.TWITCH_SERVICE_NAME);

            mBotLogin = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );

            if (mBotLogin == CommonConstants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }
        }

        // Public methods //

        public static ITwitchService Create()
        {
            return new TwitchService();
        }

        public string GetServiceDebugName()
        {
            return CommonConstants.TWITCH_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<String> {
                Service.NameOf<IIntermediaryService>(),
                Service.NameOf<IUserService>()
            };
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void Run()
        {
            // TODO should also be from Config...
            mBotToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);
            mBotToken.SetScope(new List<string>
            {
                "chat:read",
                "chat:edit",
                "user:read:email"
            });

            bool tokenFromFile = mBotToken.Loaded;
            if (!mBotToken.Loaded)
                mBotToken.Request();

            if (!Utils.IsLoginSuccessful(mBotToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mIRC = new TwitchIRC(mBotLogin, mBotToken);
            mIRC.Run();

            mIRC.AwaitLoggedIn(5000);

            mGlobalBadges = new(Utils.FetchBadges(mBotToken, null));

            LoadUserModulesFromConfig();
        }

        public void RequestShutdown()
        {
            foreach (TwitchUserModule m in mUserModules.Values)
                m.RequestShutdown();

            if (mIRC != null) mIRC.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (TwitchUserModule m in mUserModules.Values)
            {
                m.WaitForShutdown();
                m.Dispose();
            }

            if (mIRC != null) mIRC.WaitForShutdown();
        }
    }
}
