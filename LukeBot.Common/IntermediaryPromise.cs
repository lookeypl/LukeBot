using System;
using System.Threading;

namespace LukeBot.Common
{
    public class IntermediaryPromise
    {
        private AutoResetEvent mEvent = null;
        private string mReference = null;
        private bool mFulfilled; // has to be guarded by mEvent

        public PromiseData Data { get; }

        public IntermediaryPromise(string reference, ref PromiseData data)
        {
            mEvent = new AutoResetEvent(false);
            mFulfilled = false;
            mReference = reference;

            Data = data;
        }

        public bool Wait()
        {
            mEvent.WaitOne();
            return mFulfilled;
        }

        public bool Wait(int timeoutMs)
        {
            mEvent.WaitOne(timeoutMs);
            return mFulfilled;
        }

        public void Fulfill(PromiseData data)
        {
            Data.Fill(data);
            mFulfilled = true;

            mEvent.Set();
        }

        public void Reject()
        {
            mFulfilled = false;

            mEvent.Set();
        }
    }
}
