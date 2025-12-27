using LukeBot.User;

namespace LukeBot
{
    /**
     * Allows CLI parts to send messages back to currently active CLI.
     *
     * TODO maybe rename to CLIConnectionContext?
     */
    internal interface CLIMessageProxy
    {
        /**
         * Send a non-interactive message to user's interface
         */
        void Message(string msg);

        /**
         * Ask a user a yes/no question. Should return true if answered "yes"
         * or "y", false otherwise.
         */
        bool Ask(string msg);

        /**
         * Query a user for an answer. If the answer is supposed to be sensitive,
         * @p maskAnswer will be set to true.
         */
        string Query(bool maskAnswer, string msg);

        /**
         * Gets username currently associated with this connection context.
         *
         * Note that for Admin accounts this does NOT have to be the same as user
         * who logged in initially, due to "user switch" command.
         *
         * CLIProcessors should take this value and perform Commands from the
         * perspective of returned user.
         *
         * It is assumed that returned username actually exists and was validated
         * by CLIProcessor before calling SetCurrentUser().
         *
         * Should throw NoUserSelectedException when no user is selected.
         */
        IUserContext GetCurrentUser();

        /**
         * Switches user to provided context. Providing "null" as @p user is valid and is expected
         * to "unset" current user.
         *
         * Note that this action should only be done by PermissionLevel.Admin accounts.
         */
        void SetCurrentUser(IUserContext user);

        /**
         * Triggers a user data refresh.
         *
         * This should be called when user data has been modified by ex. "user update" call.
         */
        void RefreshUserData();
    }
}