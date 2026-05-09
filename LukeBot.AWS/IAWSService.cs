using LukeBot.Services;

namespace LukeBot.AWS
{
    public interface IAWSService: IService<IAWSService>
    {
        IPolly Polly();
    }
}
