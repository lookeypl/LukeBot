using System.Net;
using System.Runtime.CompilerServices;
using LukeBot.API;
using LukeBot.Logging;
using LukeBot.Twitch.Command;
using LukeBot.User;

[assembly:InternalsVisibleTo("LukeBot.Tests")]


namespace LukeBot.Twitch.Impl
{
    internal class Utils
    {
        public static bool IsLoginSuccessful(Token token)
        {
            API.Twitch.GetUserResponse data = API.Twitch.GetUserByToken(token);
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

        public static API.Twitch.GetBadgesResponse FetchBadges(Token token, string channelId)
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

            return badges;
        }

        public static ICommand AllocateChatCommand(IUserContext lbUser, Descriptor d)
        {
            ICommand cmd = null;

            switch (d.Type)
            {
            case CommandType.print: cmd = new Command.Print(d); break;
            case CommandType.shoutout: cmd = new Command.Shoutout(d); break;
            case CommandType.addcom: cmd = new Command.AddCommand(d, lbUser); break;
            case CommandType.editcom: cmd = new Command.EditCommand(d, lbUser); break;
            case CommandType.delcom: cmd = new Command.DeleteCommand(d, lbUser); break;
            case CommandType.counter: cmd = new Command.Counter(d); break;
            case CommandType.songrequest: cmd = new Command.SongRequest(d, lbUser); break;
            case CommandType.timezone: cmd = new Command.Timezone(d); break;
            default: return null;
            }

            return cmd;
        }
    }
}