using System.Collections.Generic;
using System.Threading;


namespace LukeBot.Core
{
    public enum IntercomMessageStatus
    {
        UNKNOWN = 0,
        PROCESSING,
        SUCCESS,
        ERROR,
    };

    public class IntercomMessageBase
    {
        public string Message;

        protected IntercomMessageBase(string messageTypeStr)
        {
            Message = messageTypeStr;
        }
    }

    public class IntercomResponseBase
    {
        private ManualResetEvent mWaitEvent;

        public IntercomMessageStatus Status { get; private set; }
        public string Type { get; private set; }
        public string ErrorReason { get; private set; }

        public IntercomResponseBase()
        {
            mWaitEvent = new ManualResetEvent(false);
            Status = IntercomMessageStatus.UNKNOWN;
        }

        public void Wait()
        {
            mWaitEvent.WaitOne();
        }

        public void Wait(int timeoutMs)
        {
            mWaitEvent.WaitOne(timeoutMs);
        }

        public void SignalSuccess()
        {
            Status = IntercomMessageStatus.SUCCESS;
            mWaitEvent.Set();
        }

        public void SignalError(string reason)
        {
            Status = IntercomMessageStatus.ERROR;
            ErrorReason = reason;
            mWaitEvent.Set();
        }
    };

    public delegate void IntercomDelegate(IntercomMessageBase msg, ref IntercomResponseBase resp);
    public delegate IntercomResponseBase IntercomResponseAllocator(IntercomMessageBase msg0);

    public class IntercomEndpointInfo
    {
        internal IntercomResponseAllocator mResponseAllocator;
        internal Dictionary<string, IntercomDelegate> mMethods;

        public IntercomEndpointInfo(IntercomResponseAllocator allocator)
        {
            mResponseAllocator = allocator;
            mMethods = new Dictionary<string, IntercomDelegate>();
        }

        public void AddMessage(string msg, IntercomDelegate d)
        {
            mMethods.Add(msg, d);
        }
    };

    // basic idea - replacement for CommunicationSystem
    // A wants to request something from B, but they gotta be hidden behind some abstraction
    // Provide means to:
    //  * Register as someone who awaits communication from some other side
    //  * Drop a request from A to B
    //  * Wait for response from B to A
    public class IntercomSystem
    {
        public Dictionary<string, IntercomEndpointInfo> mIntercomEndpoints;

        public IntercomSystem()
        {
            mIntercomEndpoints = new Dictionary<string, IntercomEndpointInfo>();
        }

        public void Register(string message, IntercomEndpointInfo info)
        {
            mIntercomEndpoints.Add(message, info);
        }

        public TResp Request<TMsg, TResp>(string endpoint, TMsg message)
            where TMsg: IntercomMessageBase
            where TResp: IntercomResponseBase, new()
        {
            IntercomEndpointInfo endpointInfo;
            if (!mIntercomEndpoints.TryGetValue(endpoint, out endpointInfo))
            {
                TResp r = new TResp();
                r.SignalError(string.Format("Intercom Endpoint {0} not found", endpoint));
                return r;
            }

            IntercomDelegate endpointDelegate;
            if (!endpointInfo.mMethods.TryGetValue(message.Message, out endpointDelegate))
            {
                TResp r = new TResp();
                r.SignalError(string.Format("Intercom Endpoint {0}: Message {1} not found", endpoint, message.Message));
                return r;
            }

            IntercomResponseBase resp = endpointInfo.mResponseAllocator(message);
            endpointDelegate(message, ref resp);
            return (TResp)resp;
        }
    };
}