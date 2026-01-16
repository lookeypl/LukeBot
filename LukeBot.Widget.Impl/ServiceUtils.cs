using System.Diagnostics.Contracts;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Services;
using LukeBot.Twitch;
using LukeBot.User;


namespace LukeBot.Widget.Impl
{
    internal class ServiceUtils
    {
        public static IEventService GetEventService()
        {
            return Service.Get(Common.Constants.EVENT_SERVICE_NAME) as IEventService;
        }

        public static ITwitchService GetTwitchService()
        {
            return Service.Get(Common.Constants.TWITCH_SERVICE_NAME) as ITwitchService;
        }

        public static IUserService GetUserService()
        {
            return Service.Get(Common.Constants.USER_SERVICE_NAME) as IUserService;
        }


        public static ITwitchUserModule GetTwitchUserModule(string user)
        {
            return GetTwitchService().GetModule(GetUser(user)) as ITwitchUserModule;
        }

        public static IUserContext GetUser(string user)
        {
            return GetUserService().GetUser(user);
        }
    }
}