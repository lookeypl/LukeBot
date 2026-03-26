using LukeBot.User;

namespace LukeBot.Twitch
{
    public class Utils
    {
        public static string DispatcherNameForUser(IUserContext user)
        {
            return "Twitch_SubscriberQueuedDispatcher_" + user.GetUsername();
        }
    }
}