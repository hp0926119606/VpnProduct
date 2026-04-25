using System;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Domain.Entities;

namespace VpnProduct.Application.Interfaces
{
    public interface ISyncJobService
    {
        Task<SyncJob> EnqueueNodeSyncAsync(Guid vpnNodeId, string jobType, string? payloadJson = null, CancellationToken cancellationToken = default);
        Task MarkLatestPendingJobResultAsync(Guid vpnNodeId, long configVersion, bool success, string? message, CancellationToken cancellationToken = default);
    }
}
