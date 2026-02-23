namespace LukeBot.Twitch
{
    public class Utils
    {
        public static string DispatcherNameForUser(string user)
        {
            return "Twitch_SubscriberQueuedDispatcher_" + user;
        }
    }
}