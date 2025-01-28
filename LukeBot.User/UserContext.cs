using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.User.Common;


namespace LukeBot.User
{
    internal class UserContext: IUserContext
    {
        private Guid mGUID;
        private string mUsername;

        private Dictionary<string, IUserModule> mModules = new();
        private object mLock = new();
        private PermissionLevel mPermissionLevel = PermissionLevel.None;
        private PasswordData mPasswordData = null;

        // user data management
        private void UpdateUserDataInConfig()
        {
            Path passwordDataPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_ACCOUNT_DOMAIN)
                .Push(Constants.PROP_STORE_PASSWORD);

            if (!Conf.Exists<PasswordData>(passwordDataPath))
                Conf.Add(passwordDataPath, Property.Create<PasswordData>(mPasswordData));
            else
                Conf.Modify<PasswordData>(passwordDataPath, mPasswordData);

            Path permissionLevelPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_ACCOUNT_DOMAIN)
                .Push(Constants.PROP_STORE_PERMISSION_LEVEL);

            if (!Conf.Exists<PermissionLevel>(permissionLevelPath))
                Conf.Add(permissionLevelPath, Property.Create<PermissionLevel>(mPermissionLevel));
            else
                Conf.Modify<PermissionLevel>(permissionLevelPath, mPermissionLevel);

            Conf.Save();
        }

        private void LoadUserDataFromConfig()
        {
            Path passwordDataPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_ACCOUNT_DOMAIN)
                .Push(Constants.PROP_STORE_PASSWORD);

            if (!Conf.TryGet<PasswordData>(passwordDataPath, out mPasswordData))
            {
                // no password, issue a warning
                Logger.Log().Warning("User " + mUsername + " has no password set! Remember to set your password.");
                mPasswordData = null;
            }

            Path permissionLevelPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_ACCOUNT_DOMAIN)
                .Push(Constants.PROP_STORE_PERMISSION_LEVEL);

            if (!Conf.TryGet<PermissionLevel>(permissionLevelPath, out mPermissionLevel))
            {
                // no permission level set, assume no permissions
                mPermissionLevel = PermissionLevel.None;
            }

            Logger.Log().Secure("User " + mUsername + " permission level: {0}", mPermissionLevel);
        }

        // module-config management
        private void AddModuleToConfig(string module)
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_MODULES_DOMAIN);

            ConfUtil.ArrayAppend(modulesProp, module);
        }

        private void LoadModulesFromConfig()
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_MODULES_DOMAIN);

            string[] modules;
            if (!Conf.TryGet<string[]>(modulesProp, out modules))
            {
                // Couldn't find the config entry, meaning there is no enabled modules.
                // Not considered an error.
                modules = new string[0];
            }

            foreach (string m in modules)
            {
                try
                {
                    // here we ignore the returned module and do not start it
                    // RunModules() will be called later and will kickstart it for us
                    // TODO
                    //LoadModule(m.GetModuleTypeEnum());
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to initialize module {0} for user {1}: {2}",
                        m, mUsername, e.Message);
                    Logger.Log().Error("Module {0} for user {1} will be skipped on this load.",
                        m, mUsername);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }
        }

        private void RemoveModuleFromConfig(string module)
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(mUsername)
                .Push(Constants.PROP_STORE_MODULES_DOMAIN);

            ConfUtil.ArrayRemove(modulesProp, module);
        }

        private IUserModule CreateUserModule(string type)
        {
            /*IUserModule m = Service.UserModuleManager.Create(type, mUsername);
            mModules.Add(type, m);
            return m;*/
            return null;
        }

        private void RemoveUserModule(string type)
        {
            /*IUserModule m = mModules[type];

            Service.UserModuleManager.Unload(m);
            m.RequestShutdown();
            m.WaitForShutdown();

            mModules.Remove(type);*/
        }


        public UserContext(string user)
        {
            mUsername = user;

            LoadUserDataFromConfig();

            Logger.Log().Info("Loading required modules for user {0}", mUsername);
            LoadModulesFromConfig();

            Logger.Log().Info("Loaded LukeBot user {0}", mUsername);
        }

        public Guid GetGuid()
        {
            return mGUID;
        }

        public string GetUsername()
        {
            return mUsername;
        }

        public void EnableModule(string module)
        {
            //IUserModule m;

            lock (mLock)
            {
                if (mModules.ContainsKey(module))
                {
                    throw new ModuleEnabledException(module, mUsername);
                }

                //m = CreateUserModule(module);
                AddModuleToConfig(module);

                //m.Run();
            }
        }

        public void DisableModule(string module)
        {
            lock (mLock)
            {
                if (!mModules.ContainsKey(module))
                {
                    throw new ModuleDisabledException(module, mUsername);
                }

                //RemoveUserModule(module);
                RemoveModuleFromConfig(module);
            }
        }

        public List<string> GetEnabledModules()
        {
            lock (mLock)
            {
                List<string> enabledModules = new(mModules.Keys.Count);
                foreach (string m in mModules.Keys)
                    enabledModules.Add(m);
                return enabledModules;
            }
        }

        public PermissionLevel GetPermissionLevel()
        {
            return mPermissionLevel;
        }

        // Set a new password based on a received hash. This path should
        // be taken only by remote connections (aka. via ServerCLI)
        public void SetPassword(byte[] passwordHash)
        {
            lock (mLock)
            {
                mPasswordData = PasswordData.Create(passwordHash);
                UpdateUserDataInConfig();
            }
        }

        // Set a new password based on plaintext. This path should
        // be ONLY taken locally (ex. via BasicCLI)
        public void SetPasswordLocal(string newPassword)
        {
            lock (mLock)
            {
                mPasswordData = PasswordData.Create(newPassword);
                UpdateUserDataInConfig();
            }
        }

        public void SetPermissionLevel(PermissionLevel permLevel)
        {
            lock (mLock)
            {
                mPermissionLevel = permLevel;
                UpdateUserDataInConfig();
            }
        }

        public bool ValidatePassword(byte[] passwordHash)
        {
            lock (mLock)
            {
                if (mPasswordData == null)
                {
                    // no password data - reject login
                    Logger.Log().Warning("Attempted to validate non-existing password for user {0}", mUsername);
                    return false;
                }

                return mPasswordData.Equals(passwordHash);
            }
        }

        public bool ValidatePasswordLocal(string password)
        {
            lock (mLock)
            {
                if (password.Length == 0 && mPasswordData == null)
                    return true;

                return mPasswordData.Equals(password);
            }
        }

        public void RunModules()
        {
            Logger.Log().Info("Running LukeBot modules for user {0}", mUsername);
            foreach (IUserModule m in mModules.Values)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.WaitForShutdown();
        }
    }
}
