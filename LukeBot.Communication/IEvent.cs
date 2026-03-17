using System;
using LukeBot.Common;

namespace LukeBot.Communication
{
    public interface IEvent
    {
        /**
         * Subscribe to this Event.
         *
         * @param completable Set to true if received event has to be completed by the receiving end
         *                    and confirmation has to be sent back to the System.
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
         * Subscribe to Interrupt Events. This includes both interrupting a single event and clearing the entire queue.
         */
        public void InterruptSubscribe(EventHandler<InterruptEvent> callback);

        /**
         * Unsubscribe from Interrupt Events. This includes both interrupting a single event and clearing the entire queue.
         */
        public void InterruptUnsubscribe(EventHandler<InterruptEvent> callback);
    }
}
