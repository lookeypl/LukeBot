using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.User;

using CommonConstants = LukeBot.Common.Constants;
using CommonUtils = LukeBot.Common.Utils;


namespace LukeBot.Widget.Impl
{
    public class WidgetService: IWidgetService
    {
        private Dictionary<string, WidgetUserModule> mUserModules = new();
        private Dictionary<string, string> mWidgetIDToUser = new();
        private Mutex mMutex = new();

        // Config interactions //

        private void LoadUserModulesFromConfig()
        {
            string[] users = CommonUtils.GetUserModulesFromConfig(CommonConstants.WIDGET_SERVICE_NAME);

            foreach (string u in users)
            {
                try
                {
                    CreateModule(ServiceUtils.GetUserService().GetUser(u));
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to initialize Widget user module for user {0}: {1}", u, e.Message);
                    Logger.Log().Error("Widget user module for user {0} will be skipped on this load.", u);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }
        }


        // Privates & internals //

        internal void AssignWidgetUUIDToUser(string uuid, string username)
        {
            mWidgetIDToUser.Add(uuid, username);
        }

        private WidgetUserModule LoadWidgetUserModule(string lbUser)
        {
            if (mUserModules.ContainsKey(lbUser))
            {
                throw new WidgetUserAlreadyLoadedException("Widget user {0} already loaded", lbUser);
            }

            WidgetUserModule user = new WidgetUserModule(this, lbUser);
            user.Run();

            Logger.Log().Debug("Got {0} widgets for user {1}", user.ListWidgets().Count(), lbUser);

            mUserModules.Add(lbUser, user);
            foreach (WidgetDesc wd in user.ListWidgets())
                mWidgetIDToUser.Add(wd.Id, lbUser);

            Logger.Log().Info("Loaded Widgets for user {0}", lbUser);
            return user;
        }

        private WidgetService()
        {
        }


        // IUserModuleFactory interfaces //

        public IUserModule CreateModule(IUserContext user)
        {
            WidgetUserModule module = LoadWidgetUserModule(user.GetUsername());
            CommonUtils.AddUserModuleToConfig(CommonConstants.WIDGET_SERVICE_NAME, user.GetUsername());

            return module;
        }

        public IUserModule GetModule(IUserContext user)
        {
            return mUserModules[user.GetUsername()];
        }

        public void DestroyModule(IUserContext user)
        {
            if (mUserModules.TryGetValue(user.GetUsername(), out WidgetUserModule module))
            {
                CommonUtils.RemoveUserModuleFromConfig(CommonConstants.WIDGET_SERVICE_NAME, user.GetUsername());

                module.RequestShutdown();
                module.WaitForShutdown();

                mUserModules.Remove(user.GetUsername());
            }
        }



        // Public methods //

        public static IWidgetService Create()
        {
            return new WidgetService();
        }

        public string GetServiceName()
        {
            return CommonConstants.WIDGET_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<String> {
                CommonConstants.TWITCH_SERVICE_NAME,
                CommonConstants.SPOTIFY_SERVICE_NAME,
                CommonConstants.USER_SERVICE_NAME
            };
        }

        public IWidgetUserModule GetModuleByWidgetUUID(string uuid)
        {
            return mUserModules[mWidgetIDToUser[uuid]];
        }

        public void Run()
        {
            LoadUserModulesFromConfig();
        }

        public void RequestShutdown()
        {
            foreach (WidgetUserModule um in mUserModules.Values)
                um.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (WidgetUserModule um in mUserModules.Values)
                um.WaitForShutdown();
        }
    }
}