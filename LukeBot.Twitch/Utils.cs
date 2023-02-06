using System.Net;
using LukeBot.Common;
using LukeBot.API;


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
    }
}