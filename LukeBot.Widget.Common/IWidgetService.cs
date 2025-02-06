using LukeBot.Services;
using LukeBot.User.Common;

namespace LukeBot.Widget.Common
{
    public interface IWidgetService: IService, IUserModuleFactory
    {
        public IWidgetUserModule GetModuleByWidgetUUID(string id);
    }
}