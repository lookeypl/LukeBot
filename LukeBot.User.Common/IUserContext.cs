using System;

namespace LukeBot.User.Common
{
    public interface IUserContext
    {
        public Guid GetGuid();
        public string GetUsername();
        public PermissionLevel GetPermissionLevel();

        // Enable a module
        public void EnableModule(string module);

        // Disable a module
        public void DisableModule(string module);

        // Set a new password based on a received hash. This path should
        // be taken only by remote connections (aka. via ServerCLI)
        public void SetPassword(byte[] passwordHash);

        // Set a new password based on plaintext. This path should
        // be ONLY taken locally (ex. via BasicCLI)
        public void SetPasswordLocal(string newPassword);

        public void SetPermissionLevel(PermissionLevel permLevel);

        // Validates if password is correct. For remote connections only.
        public bool ValidatePassword(byte[] passwordHash);

        // Validate if a password string is correct. Use ONLY locally.
        public bool ValidatePasswordLocal(string password);
    }
}