

using System.Data.Common;

namespace LukeBot.Communication
{
    /**
     * Interface for an Intermediary Promise, used for waiting until the
     * Promise is either fulfilled or rejected.
     */
    public interface IIntermediaryPromise
    {
        /**
         * Wait for Promise fulfillment/rejection indefinitely.
         *
         * Returns true when Promise was fulfilled, false when it was rejected.
         */
        bool Wait();

        /**
         * Wait for Promise fulfillment/rejection for @p timeoutMs milliseconds.
         *
         * Returns true when Promise was fulfilled, false when it was rejected or
         * timeout expired.
         */
        bool Wait(int timeoutMs);
    }
}
