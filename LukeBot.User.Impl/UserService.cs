using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.User;
using System;
using System.Linq;
using System.Collections.Generic;


namespace LukeBot.User.Impl
{
    public class UserService: IUserService
    {
        private Dictionary<string, UserContext> mUsers = new();
        private object mUsersLock = new();

        private void AddUserToConfig(string name)
        {
            ConfUtil.ArrayAppend(Constants.PROP_STORE_USERS_PROP, name);
        }

        private void RemoveUserFromConfig(string name)
        {
            if (!mUsers.ContainsKey(name))
            {
                throw new ArgumentException("User " + name + " does not exist.");
            }

            ConfUtil.ArrayRemove(Constants.PROP_STORE_USERS_PROP, name);

            // also clear entire branch of user-related settings
            Path userConfDomain = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(name);

            if (Conf.Exists(userConfDomain))
                Conf.Remove(userConfDomain);
        }

        private void CreateUser(string username)
        {
            if (mUsers.ContainsKey(username) || username == Constants.LUKEBOT_USER_ID)
                throw new UsernameNotAvailableException(username);

            Comms.Event.AddUser(username);
            mUsers.Add(username, new UserContext(username));
        }

        private UserService()
        {
        }


        public static IUserService Create()
        {
            return new UserService();
        }


        public string GetServiceName()
        {
            return Constants.USER_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return null;
        }

        public void LoadUsers()
        {
            Path usersProp = Constants.PROP_STORE_USERS_PROP;

            if (!Conf.Exists(usersProp))
            {
                Logger.Log().Info("No users found");
                return;
            }

            string[] users = Conf.Get<string[]>(usersProp);

            if (users.Length == 0)
            {
                Logger.Log().Info("Users array is empty");
                return;
            }

            foreach (string user in users)
            {
                Logger.Log().Info("Loading LukeBot user " + user);
                CreateUser(user);
            }
        }

        public void UnloadUsers()
        {
            Logger.Log().Info("Unloading users...");

            foreach (UserContext u in mUsers.Values)
            {
                u.RequestModuleShutdown();
            }

            foreach (UserContext u in mUsers.Values)
            {
                u.WaitForModulesShutdown();
            }

            mUsers.Clear();
        }

        /**
         * Will be called if there is an attempt to authenticate a user.
         *
         * Example: ServerCLI receives a Login message, and during processing will call
         * this method to check if password is correct.
         *
         * Returns user's context if authentication succeeded. In case of auth failure
         * should return null.
         */
        public IUserContext AuthenticateUser(string user, byte[] pwdHash, out string reason)
        {
            UserContext ctx;

            lock (mUsersLock)
            {
                if (!mUsers.TryGetValue(user, out ctx))
                {
                    reason = "User not found";
                    return null;
                }
            }

            if (!ctx.ValidatePassword(pwdHash))
            {
                reason = "Invalid password";
                return null;
            }

            reason = "";
            return ctx;
        }

        /**
         * Will be called if there is an attempt to change user's password.
         *
         * Example: ServerCLI receives a PasswordChange message, and during processing will
         * call this method to check if password can be changed.
         *
         * Should return true upon success and false upon failure. Additionally, @p reason
         * should be set when authentication fails to provide a reason why.
         *
         * TODO this should be IUserContext API
         */
        public bool ChangeUserPassword(string user, byte[] currentPwdHash, byte[] newPwdHash, out string reason)
        {
            if (AuthenticateUser(user, currentPwdHash, out reason) == null)
                return false;

            lock (mUsersLock)
            {
                mUsers[user].SetPassword(newPwdHash);
            }

            reason = "";
            return true;
        }

        public IUserContext GetUser(string username)
        {
            lock (mUsersLock)
            {
                return mUsers[username];
            }
        }

        public void CreateNewUser(string username)
        {
            lock (mUsersLock)
            {
                CreateUser(username);
                AddUserToConfig(username);
            }
        }

        public void RemoveUser(string lbUsername)
        {
            lock (mUsersLock)
            {
                UserContext u = mUsers[lbUsername];
                u.RequestModuleShutdown();
                u.WaitForModulesShutdown();

                RemoveUserFromConfig(lbUsername);
                mUsers.Remove(lbUsername);
                Comms.Event.RemoveUser(lbUsername);
            }
        }

        public List<string> GetUsernames()
        {
            lock (mUsersLock)
            {
                return mUsers.Keys.ToList<string>();
            }
        }

        // returns true if @p username exists, or is empty; false otherwise
        public bool IsUsernameValid(string username)
        {
            lock (mUsersLock)
            {
                return username.Length != 0 && mUsers.ContainsKey(username);
            }
        }

        public void Run()
        {
            LoadUsers();
        }

        public void RequestShutdown()
        {
            // noop
        }

        public void WaitForShutdown()
        {
            // noop
        }
    }
}