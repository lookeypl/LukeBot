using LukeBot.Common;

namespace LukeBot.Filesystem.Impl
{
    public class FilesystemUserModule: IFilesystemUserModule
    {
        public void Run()
        {
        }

        public void RequestShutdown()
        {
        }

        public void WaitForShutdown()
        {
        }

        public string GetModuleType()
        {
            return Constants.FILESYSTEM_SERVICE_NAME;
        }
    }
}
