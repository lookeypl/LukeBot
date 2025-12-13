using LukeBot.Services;

namespace LukeBot.Filesystem
{
    /**
     * Service responsible for managing files - larger resources that
     * backends might need for any reason.
     *
     * This exposes the API to access global (bot-wide) files. In most
     * cases the user-specific files should be used instead.
     */
    public interface IFilesystemService: IService
    {

    }
}
