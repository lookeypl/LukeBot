using System.Net;
using LukeBot.API;
using LukeBot.Logging;


namespace LukeBot.Twitch
{
    internal class Utils
    {
        public static bool IsLoginSuccessful(Token token)
        {
            API.Twitch.GetUserResponse data = API.Twitch.GetUser(token);
            if (data.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Twitch login successful");
                return true;
            }
            else if (data.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Twitch - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Twitch: " + data.code.ToString());
        }

        public static BadgeCollection FetchBadgeCollection(Token token, string channelId)
        {
            API.Twitch.GetBadgesResponse badges = null;
            if (channelId == null || channelId.Length == 0)
            {
                // fetch global badges
                badges = API.Twitch.GetGlobalChatBadges(token);
            }
            else
            {
                // fetch channel badges
                badges = API.Twitch.GetChannelChatBadges(token, channelId);
            }

            if (!badges.IsSuccess)
            {
                throw new System.Exception(string.Format("Failed to get badge collection: {0}", badges.code.ToString()));
            }

            return new BadgeCollection(badges);
        }
    }
}