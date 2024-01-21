
namespace LukeBot.Communication.Common
{
    public abstract class EventArgsBase
    {
        public string EventName { get; private set; }

        public EventArgsBase(string name)
        {
            EventName = name;
        }
    }
}