using System.Threading;
using Microsoft.Extensions.Hosting;

namespace LukeBot.Endpoint
{
    class HostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => throw new System.NotImplementedException();

        public CancellationToken ApplicationStopped => throw new System.NotImplementedException();

        public CancellationToken ApplicationStopping => throw new System.NotImplementedException();

        public void StopApplication()
        {
            throw new System.NotImplementedException();
        }
    }
}