using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Domain.Entities;

namespace VpnProduct.Application.Interfaces
{
    public interface IVpnPeerService
    {
        Task<List<VpnPeer>> GetByNodeIdAsync(Guid vpnNodeId, CancellationToken cancellationToken = default);
        Task<VpnPeer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<VpnPeer> CreateAsync(VpnPeer peer, CancellationToken cancellationToken = default);
        Task<VpnPeer> UpdateAsync(Guid id, VpnPeer peer, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
