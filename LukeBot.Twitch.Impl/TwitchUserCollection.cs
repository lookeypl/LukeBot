using System.Collections.Generic;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Logging;

namespace LukeBot.Twitch.Impl
{
    internal class TwitchUserCollection
    {
        private Dictionary<string, TwitchUserIdentity> mUsers = new();
        private Dictionary<string, string> mUsernameToID = new();

        public TwitchUserCollection()
        {
        }

        TwitchUserIdentity ID(string id)
        {
            return mUsers[id];
        }

        TwitchUserIdentity Username(string username)
        {
            return ID(mUsernameToID[username]);
        }

        private void AddUser(TwitchUserIdentity identity)
        {
            string id = identity.UserData.id;
            mUsers.Add(id, identity);
            mUsernameToID.Add(identity.UserData.login, id);
        }

        public TwitchUserIdentity FetchUser(Token apiToken, bool ignoreExisting, string username)
        {
            return FetchUsers(apiToken, ignoreExisting, new string[] { username })[0];
        }

        public TwitchUserIdentity[] FetchUsers(Token apiToken, bool ignoreExisting, string[] usernames)
        {
            API.Twitch.GetUserResponse resp = API.Twitch.GetUsersByLogin(apiToken, usernames);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Error("Failed to fetch user data from Twitch - received error code {0}", resp.code.ToString());
                throw new APIResponseErrorException(resp.code);
            }

            List<TwitchUserIdentity> result = new();
            foreach (API.Twitch.GetUserData userData in resp.data)
            {
                if (mUsers.ContainsKey(userData.id))
                {
                    if (!ignoreExisting)
                    {
                        throw new UserIdentityExistsException(userData.login, userData.id);
                    }
                }
                else
                {
                    TwitchUserIdentity identity = new(userData);
                    AddUser(identity);
                    result.Add(identity);
                }
            }

            return result.ToArray();
        }
    };
}
