
namespace LukeBot.Communication.Events
{
    public class EventUtils
    {
        public static System.Type GetEventTypeArgs(string typeStr)
        {
            return System.Type.GetType("LukeBot.Communication.Events." + typeStr + "Args");
        }
    }
}