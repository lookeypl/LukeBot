using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LukeBot.Common;


namespace LukeBot.Communication
{
    public sealed class Intermediary
    {
        private Dictionary<string, IntermediaryPromise> mPromises = new Dictionary<string, IntermediaryPromise>();

        public Intermediary()
        {
        }

        ~Intermediary()
        {
        }

        // Warn Intermediary about upcoming communication
        public IntermediaryPromise Expect(string reference, ref PromiseData data)
        {
            IntermediaryPromise promise = new IntermediaryPromise(reference, ref data);

            mPromises.Add(reference, promise);

            return promise;
        }

        // Complete the communication successfully with results
        public void Fulfill(string reference, PromiseData data)
        {
            if (!mPromises.ContainsKey(reference))
                throw new InvalidOperationException(String.Format("Promise referenced by {0} not found", reference));

            IntermediaryPromise promise;
            mPromises.Remove(reference, out promise);
            promise.Fulfill(data);
        }

        // Fail to complete communication
        public void Reject(string reference)
        {
            if (!mPromises.ContainsKey(reference))
                throw new InvalidOperationException(String.Format("Promise referenced by {0} not found", reference));

            IntermediaryPromise promise;
            mPromises.Remove(reference, out promise);
            promise.Reject();
        }
    }
}
