using LukeBot.Module;
using LukeBot.User.Common;

namespace LukeBot.User
{
    public class UserService: IService
    {
        /**
         * Will be called if there is an attempt to authenticate a user.
         *
         * Example: ServerCLI receives a Login message, and during processing will call
         * this method to check if password is correct.
         *
         * Returns user's Permission level if authentication succeeded. In case of auth failure
         * should return UserPermissionLevel.None.
         */
        public PermissionLevel AuthenticateUser(string user, byte[] pwdHash, out string reason)
        {
            // TODO IMPLEMENT
            reason = "TODO";
            return PermissionLevel.None;
        }

        /**
         * Will be called if there is an attempt to change user's password.
         *
         * Example: ServerCLI receives a PasswordChange message, and during processing will
         * call this method to check if password can be changed.
         *
         * Should return true upon success and false upon failure. Additionally, @p reason
         * should be set when authentication fails to provide a reason why.
         */
        public bool ChangeUserPassword(string user, byte[] currentPwdHash, byte[] newPwdHash, out string reason)
        {
            // TODO IMPLEMENT
            reason = "TODO";
            return false;
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            throw new System.NotImplementedException();
        }
    }
}