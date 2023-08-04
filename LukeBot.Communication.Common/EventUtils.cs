
namespace LukeBot.Communication.Common
{
    public class EventUtils
    {
        public static System.Type GetEventTypeArgs(string typeStr)
        {
            return System.Type.GetType("LukeBot.Communication.Common." + typeStr + "Args");
        }
    }
}