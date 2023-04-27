using LukeBot.Common;

namespace LukeBot
{
    public class NoUserSelectedException: Exception
    {
        public NoUserSelectedException()
            : base("No user selected")
        {
        }
    }
}