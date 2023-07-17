using LukeBot.Common;

namespace LukeBot.Interface
{
    public class InterfaceNotInitializedException: Exception
    {
        public InterfaceNotInitializedException()
            : base("Interface is not initialized")
        {
        }
    }
}
