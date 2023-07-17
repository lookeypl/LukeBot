using LukeBot.Common;

namespace LukeBot
{
    public class UsernameNotAvailableException: Exception
    {
        public UsernameNotAvailableException(string lbUsername)
            : base("Username not available: " + lbUsername)
        {
        }
    }
}