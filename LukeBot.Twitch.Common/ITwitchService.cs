using System.Collections.Generic;
using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Twitch.Common
{
    public interface ITwitchService: IService, IUserModuleFactory
    {
        public void AwaitIRCLoggedIn(int timeoutMs);
    }
}