using LukeBot.User.Common;

namespace LukeBot.Filesystem
{
    /**
     * Exposes API to manage user-space files.
     *
     * Assumes that each user's filesystem is separate - user A cannot view files of user B.
     */
    public interface IFilesystemUserModule: IUserModule
    {

    }
}
