using System;
using LukeBot.Services;

namespace LukeBot.User.Common
{
    public interface IUserContext
    {
        public Guid GetGuid();
        public string GetUsername();
        public PermissionLevel GetPermissionLevel();

        // Attaches a User Module
        // Should be done by Services when creating a new UserModule
        public void AttachModule(IUserModule module);

        // Detaches a user module
        public void DetachModule(IUserModule module);

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