using LukeBot.Services;

namespace LukeBot.Communication
{
    public interface IEventService: IService
    {
        void AddUser(string lbUser);
        void RemoveUser(string lbUser);
        IEventCollection User(string lbUser);
        IEventCollection Global();
    }
}
