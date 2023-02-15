
namespace LukeBot.Communication.Events
{
    public class EventArgsBase
    {
        public Events.Type eventType { get; private set; }
        public string Type { get; private set; }

        public EventArgsBase(Events.Type type, string typeStr)
        {
            eventType = type;
            Type = typeStr;
        }
    }
}