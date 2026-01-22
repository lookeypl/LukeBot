using System.Diagnostics.Contracts;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Services;
using LukeBot.Twitch;
using LukeBot.User;


namespace LukeBot.Twitch.Impl
{
    internal class ServiceUtils
    {
        public static IEventService GetEventService()
        {
            return Service.Get<IEventService>();
        }

        public static IIntermediaryService GetIntermediaryService()
        {
            return Service.Get<IIntermediaryService>();
        }

        public static IUserService GetUserService()
        {
            return Service.Get<IUserService>();
        }
    }
}