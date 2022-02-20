using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace LukeBot.Endpoint
{
    class HostLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}