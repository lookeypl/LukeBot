using LukeBot.Services;


namespace LukeBot.Communication
{
    /**
     * Service for managing Intermediaries - objects which serve as a middle-man
     * for bits and pieces that require something from ex. the Endpoint.
     *
     * Currently main use-case for this is OAuth flow in LukeBot.API communicating
     * with LukeBot.Endpoint - OAuth token might need to be acquired, which requires
     * redirecting to a login page and making a Promise in this service. After login
     * is successful external service redirects to the web callback which is captured
     * by the Endpoint. Endpoint fulfills the Promise providing API with Token information
     * for the future.
     */
    public interface IIntermediaryService: IService
    {
        /**
         * Register a new Service that will be expecting some data from other
         * modules.
         *
         * Intermediary for this service can then be acquired via GetIntermediary()
         */
        void Register(string service);

        /**
         * Get the Intermediary for a registered Service.
         */
        IIntermediary GetIntermediary(string service);
    }
}