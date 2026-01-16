using System;
using LukeBot.Common;

namespace LukeBot.Communication
{
    public interface IEvent
    {
        public void Subscribe(EventHandler<EventArgsBase> callback);
        public void Unsubscribe(EventHandler<EventArgsBase> callback);
        public void InterruptSubscribe(EventHandler<EventArgsBase> callback);
        public void InterruptUnsubscribe(EventHandler<EventArgsBase> callback);
    }
}
