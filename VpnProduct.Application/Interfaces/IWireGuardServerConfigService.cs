using System;
using System.Threading;
using System.Threading.Tasks;

namespace VpnProduct.Application.Interfaces
{
    public interface IWireGuardServerConfigService
    {
        Task<string> BuildServerConfigAsync(Guid nodeId, CancellationToken cancellationToken = default);
    }
}
