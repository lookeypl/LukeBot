using LukeBot.Common;

namespace LukeBot.Twitch.Impl
{
    public class UserIdentityExistsException: Exception
    {
        public UserIdentityExistsException(string username, string id)
            : base(string.Format("Twitch User Identity {0} ({1}) already exists in the Collecton", username, id))
        {}
    }
}
