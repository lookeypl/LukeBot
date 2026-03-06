using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Services;

namespace LukeBot.Widget.Impl
{
    /**
     * Widget specialization that assumes Events will be queued on Widget-side.
     *
     * Note that for best results these widgets should work only with SubscriberQueued Event Dispatchers.
     */
    public abstract class QueueableEventWidget : IWidget
    {
        private Dictionary<Guid, SerializableEventArgsBase> mSentEvents = new();
        private object mResponseReceiveLock = new();
        private bool mWaitingForResponse = false;
        private TaskCompletionSource<WidgetResponse> mResponseCS = null;
        private Guid mExpectingResponseFor;

        private async Task<WidgetResponse> WaitForResponse()
        {
            lock (mResponseReceiveLock)
            {
                if (!mWaitingForResponse || mResponseCS == null)
                {
                    throw new WidgetSystemException("Widget {0}: Response wait accessed when it shouldn't be, something is really wrong.", GetPrintableWidgetID());
                }
            }

            await mResponseCS.Task;

            WidgetResponse resp = null;

            lock (mResponseReceiveLock)
            {
                if (mResponseCS.Task.Result == null)
                {
                    throw new WidgetSystemException("Widget {0}: Received response is NULL when it shouldn't be, something went wrong", GetPrintableWidgetID());
                }

                // double-check if the response we got is correct
                if (mResponseCS.Task.Result.EventID != mExpectingResponseFor)
                {
                    throw new WidgetSystemException("Widget {0}: Received response Guid {1} doesn't match expceted {2}, something went wrong",
                        GetPrintableWidgetID(), mResponseCS.Task.Result.EventID, mExpectingResponseFor
                    );
                }

                resp = mResponseCS.Task.Result;
                LogResponseStatus(resp);

                mResponseCS = null;
            }

            return resp;
        }

        private void SendInterruptEvent(object o, InterruptEvent interruptEvent)
        {
            SendEventAndWait(interruptEvent);
        }

        /**
         * Common function to log the response from the Widget. Use this to print out what happened
         * with the event that was sent (assuming you use SendEventAndWait)
         */
        protected void LogResponseStatus(WidgetResponse response)
        {
            if (response.ErrorCount == 0)
            {
                Logger.Log().Debug("Widget {0}: Completed event {1} successfully", GetPrintableWidgetID(), response.EventID);
            }
            else if (response.ErrorCount == 1)
            {
                Logger.Log().Warning("Widget {0}: failed to complete the event: {1}", GetPrintableWidgetID(), response.Reason[0]);
            }
            else
            {
                Logger.Log().Warning("Widget {0}: {1} errors occured during event completion attempt:", GetPrintableWidgetID(), response.ErrorCount);
                for (int i = 0; i < response.Reason.Length; ++i)
                {
                    Logger.Log().Warning("  - {0}", response.Reason[i]);
                }
            }
        }

        protected QueueableEventWidget(string lbUser, string widgetFilePath, string id, string name)
            : base(lbUser, widgetFilePath, id, name)
        {
        }

        /**
         * Sends an Event to Widget's WebSocket. This will leave after the Event
         * has been sent. The Event itself will be saved for later completion reporting.
         *
         * With this path, Widget will (eventually) respond with the status of the Event.
         * Response is handled by OnReceivedResponse().
         *
         * If you need to synchronously send an Event and await until Widget reports its
         * completion, use @p SendEventAndWaitAsync or @p SendEventAndWait.
         */
        protected async Task SendEventAsync<T>(T obj)
            where T: SerializableEventArgsBase
        {
            mSentEvents.Add(obj.EventID, obj);
            Logger.Log().Debug("Widget {0}: Sending to client event {1} {2}", GetPrintableWidgetID(), obj.EventName, obj.EventID);
            await SendToWSAsync(obj);
        }

        /**
         * Sends an Event to Widget's WebSocket. See @p SendEventAsync for more details.
         */
        protected void SendEvent<T>(T obj)
            where T: SerializableEventArgsBase
        {
            Task t = SendEventAsync(obj);
            t.Wait();
        }

        /**
         * Sends an Event to Widget's WebSocket. After sending it will block current Thread
         * until Widget sends back a Response, which is then returned.
         *
         * This method should be used in cases where an immediate answer from the Widget is expected.
         * A most common use case is sending a Widget Configuration object to initialize it and then
         * receive a response confirming the Configuration was received and applied.
         */
        protected async Task<WidgetResponse> SendEventAndWaitAsync<T>(T obj)
            where T: SerializableEventArgsBase
        {
            lock (mResponseReceiveLock)
            {
                mWaitingForResponse = true;
                mResponseCS = new();
                mExpectingResponseFor = obj.EventID;
            }

            bool success = await SendToWSAsync(obj);
            if (!success)
            {
                lock (mResponseReceiveLock)
                {
                    mWaitingForResponse = false;
                    mResponseCS = null;
                }

                return null;
            }

            return await WaitForResponse();
        }

        /**
         * Sends an Event to Widget's WebSocket and blocks until a Response is received.
         * See @p SendEventAndWaitAsync for more details.
         */
        protected WidgetResponse SendEventAndWait<T>(T obj)
            where T: SerializableEventArgsBase
        {
            Task<WidgetResponse> t = SendEventAndWaitAsync(obj);
            t.Wait();
            return t.Result;
        }

        protected void EventSubscribe(string eventName, EventHandler<EventArgsBase> handler, bool interruptable)
        {
            IEvent ev = ServiceUtils.GetEventService().User(mLBUser).Event(eventName);

            ev.Subscribe(handler, true);

            if (interruptable)
            {
                ev.InterruptSubscribe(SendInterruptEvent);
            }
        }

        protected void EventUnsubscribe(string eventName, EventHandler<EventArgsBase> handler)
        {
            IEvent ev = ServiceUtils.GetEventService().User(mLBUser).Event(eventName);

            ev.Unsubscribe(handler);
            ev.InterruptUnsubscribe(SendInterruptEvent);
        }

        // accessed by receive thread in IWidget
        protected sealed override void OnReceivedResponse(WidgetResponse response)
        {
            Logger.Log().Debug("Widget {0}: Receive callback picks up response {1}", GetPrintableWidgetID(), response.EventID);

            lock (mResponseReceiveLock)
            {
                // separate path for when we are immediately awaiting a response
                // if we get a response that's not matching Guid of what we expect to receive
                // this condition will continue on to add it to regular processing loop
                if (mWaitingForResponse && mResponseCS != null &&
                    response.EventID == mExpectingResponseFor)
                {
                    Logger.Log().Debug("Widget {0}: Response expected via SendEventAndWait", GetPrintableWidgetID());
                    mWaitingForResponse = false;
                    mResponseCS.SetResult(response);
                    return;
                }
            }

            if (mSentEvents.TryGetValue(response.EventID, out SerializableEventArgsBase evArgs))
            {
                Logger.Log().Debug("Widget {0}: Response registered, processing", GetPrintableWidgetID());
                LogResponseStatus(response);

                // mark event as completed and remove it regardless of the outcome
                // this is to clean up on Event System side
                evArgs.Completed();
                mSentEvents.Remove(response.EventID);
            }
            else
            {
                Logger.Log().Warning("Widget {0}: Receive callback got a Response that is not registered, ignoring", GetPrintableWidgetID());
            }
        }
    }
}