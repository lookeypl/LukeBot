using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<string> KnownUsers()
        {
            return mUsers.Values.Select((uid) => uid.Username);
        }

        public TwitchUserIdentity FetchUser(Token apiToken, bool ignoreExisting, string username)
        {
            return FetchUsers(apiToken, ignoreExisting, new string[] { username })[0];
        }

        public TwitchUserIdentity[] FetchUsers(Token apiToken, bool ignoreExisting, string[] usernames)
        {
            API.Twitch.GetUserResponse resp = API.Twitch.GetUsersByLogin(apiToken, usernames);
            if (!resp.IsSuccess)
            {
                Logger.Log().Error("Failed to fetch user data from Twitch - received error code {0}", resp.code.ToString());
                throw new APIResponseErrorException(resp.code);
            }

            string[] ids = resp.data.Select((ud) => ud.id).ToArray();
            List<API.Twitch.UserChatColorData> chatColors = null;
            API.Twitch.GetUserChatColorResponse colorsResp = API.Twitch.GetUsersChatColor(apiToken, ids);
            if (!colorsResp.IsSuccess)
            {
                Logger.Log().Warning("Failed to fetch users chat colors - {0} ({1}); will ignore them until fetched in a different way",
                    (int)colorsResp.code, colorsResp.code.ToString()
                );
            }
            else
            {
                chatColors = colorsResp.data;
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
                    if (chatColors != null)
                    {
                        API.Twitch.UserChatColorData colorData = chatColors.SingleOrDefault((color) => color.user_id == userData.id);
                        if (colorData != null)
                        {
                            identity.Color = colorData.color;
                        }
                    }
                    AddUser(identity);
                    result.Add(identity);
                }
            }

            return result.ToArray();
        }

        public void FetchChatters(Token token, string channelId)
        {
            List<API.Twitch.UserData> chatters = API.Twitch.GetChatters(token, channelId);

            int current = 0;
            int step = 100;
            while (current < chatters.Count)
            {
                string[] ids = chatters.Skip(current).Take(step).Select((ud) => ud.user_login).ToArray();
                current += step;

                FetchUsers(token, true, ids);
            }
        }
    };
}
