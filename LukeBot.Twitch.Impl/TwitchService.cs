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
        private API.Twitch.GetUserResponse mBotData;
        private Dictionary<string, TwitchUserModule> mUserModules = new();


        // Other Services interactions //

        private IIntermediaryService GetIntermediaryService()
        {
            return Service.Get(CommonConstants.INTERMEDIARY_SERVICE_NAME) as IIntermediaryService;
        }


        // Config interactions //

        private void LoadUserModulesFromConfig()
        {
            string[] users = CommonUtils.GetUserModulesFromConfig(CommonConstants.TWITCH_SERVICE_NAME);

            foreach (string u in users)
            {
                try
                {
                    IUserService userService = Service.Get(CommonConstants.USER_SERVICE_NAME) as IUserService;
                    CreateModule(userService.GetUser(u));
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

        private TwitchUserModule JoinChannel(string lbUser)
        {
            string channel = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.TWITCH_SERVICE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );

            if (mUserModules.ContainsKey(lbUser))
            {
                throw new ChannelAlreadyJoinedException(lbUser);
            }

            Logger.Log().Debug("Joining {0} channel for user {1}", channel, lbUser);

            TwitchUserModule module = null;

            try
            {
                module = new TwitchUserModule(lbUser, mBotToken, mIRC);
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

            Logger.Log().Secure("Joined channel twitch ID: {0}", module.GetUserData().id);
            return module;
        }

        private void PartChannel(string lbUser)
        {
            if (mUserModules.TryGetValue(lbUser, out TwitchUserModule module))
            {
                Logger.Log().Debug("Parting channel {0} for user {1}", module.GetChannelName(), lbUser);

                try
                {
                    module.Dispose();
                }
                finally
                {
                    mUserModules.Remove(lbUser);
                }

                Logger.Log().Secure("Parted channel twitch ID: {0} ", module.GetUserData().id);
            }
        }


        // IUserModuleFactory //

        public IUserModule CreateModule(IUserContext user)
        {
            TwitchUserModule module = JoinChannel(user.GetUsername());
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
            GetIntermediaryService().Register(CommonConstants.TWITCH_SERVICE_NAME);

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

        public string GetServiceName()
        {
            return CommonConstants.TWITCH_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<String> {
                CommonConstants.INTERMEDIARY_SERVICE_NAME,
                CommonConstants.USER_SERVICE_NAME
            };
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void Run()
        {
            string tokenScope = "chat:read chat:edit user:read:email"; // TODO should also be from Config...
            mBotToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);

            bool tokenFromFile = mBotToken.Loaded;
            if (!mBotToken.Loaded)
                mBotToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mBotToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.Twitch.GetUser(mBotToken);
            mIRC = new TwitchIRC(mBotLogin, mBotToken);
            mIRC.Run();

            mIRC.AwaitLoggedIn(5000);

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
