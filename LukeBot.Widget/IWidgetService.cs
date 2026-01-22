using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Widget
{
    public interface IWidgetService: IService<IWidgetService>, IUserModuleFactory
    {
        public IWidgetUserModule GetModuleByWidgetUUID(string id);
    }
}