using System.Threading;
using System.Collections.Generic;


namespace LukeBot.Communication.Common
{
    namespace Intercom
    {
        public enum MessageStatus
        {
            UNKNOWN = 0,
            PROCESSING,
            SUCCESS,
            ERROR,
        };

        public class MessageBase
        {
            public string Endpoint;
            public string Message;

            protected MessageBase(string endpointStr, string messageTypeStr)
            {
                Endpoint = endpointStr;
                Message = messageTypeStr;
            }
        }

        public class ResponseBase
        {
            private ManualResetEvent mWaitEvent;

            public MessageStatus Status { get; private set; }
            public string Type { get; private set; }
            public string ErrorReason { get; private set; }

            public ResponseBase()
            {
                mWaitEvent = new ManualResetEvent(false);
                Status = MessageStatus.UNKNOWN;
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
                Status = MessageStatus.SUCCESS;
                mWaitEvent.Set();
            }

            public void SignalError(string reason)
            {
                Status = MessageStatus.ERROR;
                ErrorReason = reason;
                mWaitEvent.Set();
            }
        };

        public delegate void Delegate(MessageBase msg, ref ResponseBase resp);
        public delegate ResponseBase ResponseAllocator(MessageBase msg0);

        public class EndpointInfo
        {
            public string mName { get; private set; }
            public ResponseAllocator mResponseAllocator { get; private set; }
            public Dictionary<string, Delegate> mMethods { get; private set; }

            private static ResponseBase DefaultResponseAllocator(MessageBase msg)
            {
                return new ResponseBase();
            }

            public EndpointInfo(string name)
                : this(name, DefaultResponseAllocator)
            {
            }

            public EndpointInfo(string name, ResponseAllocator allocator)
            {
                mName = name;
                mResponseAllocator = allocator;
                mMethods = new Dictionary<string, Delegate>();
            }

            public void AddMessage(string msg, Delegate d)
            {
                mMethods.Add(msg, d);
            }
        };
    }
}