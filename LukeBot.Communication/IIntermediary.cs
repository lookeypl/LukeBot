namespace LukeBot.Communication
{
    public interface IIntermediary
    {
        /**
         * Form a Promise, passing along a @p data object which will be filled
         * by another module (most probably Endpoint).
         *
         * @p reference contains a (most probably) random String which will be
         * used to identify the Promise on the Endpoint side.
         *
         * Returns an IIntermediaryPromise object which allows to wait until
         * the Promise is fulfilled.
         *
         * Most commonly called by other modules that might require something
         * from the Endpoint.
         */
        IIntermediaryPromise Expect(string reference, ref PromiseData data);

        /**
         * Fulfill a Promise created with the @p reference ref string.
         *
         * This function will also fill a PromiseData object based on @p data.
         */
        void Fulfill(string reference, PromiseData data);

        /**
         * Reject a Promise with the @p reference ref string.
         *
         * This will inform the expecting side that the Promise was rejected for
         * whatever reason (most commonly an error, or the login process being
         * cancelled)
         */
        void Reject(string reference);
    }
}
