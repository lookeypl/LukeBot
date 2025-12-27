using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Widget.Common
{
    public interface IWidgetService: IService, IUserModuleFactory
    {
        public IWidgetUserModule GetModuleByWidgetUUID(string id);
    }
}