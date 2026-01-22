using System.Collections.Generic;
using LukeBot.Services;


namespace LukeBot.User
{
    public interface IUserService: IService<IUserService>
    {
        public void LoadUsers();
        public void UnloadUsers();
        public IUserContext AuthenticateUser(string user, byte[] pwdHash, out string reason);
        public bool ChangeUserPassword(string user, byte[] currentPwdHash, byte[] newPwdHash, out string reason);
        public IUserContext GetUser(string username);
        public void CreateNewUser(string username);
        public void RemoveUser(string lbUsername);
        public List<string> GetUsernames();
        public bool IsUsernameValid(string username);
    }
}