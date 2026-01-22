using System.Collections.Generic;
using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Twitch
{
    public interface ITwitchService: IService<ITwitchService>, IUserModuleFactory
    {
        public void AwaitIRCLoggedIn(int timeoutMs);
    }
}