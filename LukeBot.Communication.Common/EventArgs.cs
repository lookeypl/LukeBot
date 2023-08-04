
namespace LukeBot.Communication.Common
{
    public class EventArgsBase
    {
        public string Type  { get; private set; }

        public EventArgsBase(string typeStr)
        {
            Type = typeStr;
        }
    }

    public class UserEventArgsBase: EventArgsBase
    {
        public UserEventType eventType { get; private set; }

        public UserEventArgsBase(UserEventType type, string typeStr)
            : base(typeStr)
        {
            eventType = type;
        }
    }

    public class GlobalEventArgsBase: EventArgsBase
    {
        public GlobalEventType eventType  { get; private set; }

        public GlobalEventArgsBase(GlobalEventType type, string typeStr)
            : base(typeStr)
        {
            eventType = type;
        }
    }
}