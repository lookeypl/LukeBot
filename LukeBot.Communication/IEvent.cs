using System;
using LukeBot.Common;

namespace LukeBot.Communication
{
    public interface IEvent
    {
        /**
         * Subscribe to this Event.
         *
         * @param completable Set to true if received event has to be completed by the receiving end.
         *                    After receiving the event subscriber will have to call
         *                    EventArgsBase.Complete() confirming the event has been completed
         *                    successfully. See EventArgsBase for details.
         */
        public void Subscribe(EventHandler<EventArgsBase> callback, bool completable = false);

        /**
         * Unsubscribe from this Event.
         */
        public void Unsubscribe(EventHandler<EventArgsBase> callback);

        /**
         * Subscribe to Interrupt Events.
         */
        public void InterruptSubscribe(EventHandler<InterruptEvent> callback);

        /**
         * Unsubscribe from Interrupt Events
         */
        public void InterruptUnsubscribe(EventHandler<InterruptEvent> callback);
    }
}
