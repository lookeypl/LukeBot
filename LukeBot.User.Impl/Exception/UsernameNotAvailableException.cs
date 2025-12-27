using LukeBot.Common;

namespace LukeBot.User.Impl
{
    public class UsernameNotAvailableException: Exception
    {
        public UsernameNotAvailableException(string lbUsername)
            : base("Username not available: " + lbUsername)
        {
        }
    }
}