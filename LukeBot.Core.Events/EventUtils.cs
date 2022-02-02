
namespace LukeBot.Core.Events
{
    public class EventUtils
    {
        public static System.Type GetEventTypeArgs(string typeStr)
        {
            return System.Type.GetType("LukeBot.Core.Events." + typeStr + "Args");
        }
    }
}