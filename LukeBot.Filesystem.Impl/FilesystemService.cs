using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.Filesystem.Impl
{
    public class FilesystemService: IFilesystemService
    {
        public IEnumerable<string> GetServiceDependencies()
        {
            return new[] {
                Constants.USER_SERVICE_NAME
            };
        }

        public string GetServiceName()
        {
            return Constants.FILESYSTEM_SERVICE_NAME;
        }

        public void Run()
        {

        }

        public void RequestShutdown()
        {

        }

        public void WaitForShutdown()
        {

        }
    }
}
