using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Application.Interfaces;
using VpnProduct.Domain.Entities;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Infrastructure.Services
{
    public class VpnPeerService : IVpnPeerService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISyncJobService _syncJobService;

        public VpnPeerService(ApplicationDbContext dbContext, ISyncJobService syncJobService)
        {
            _dbContext = dbContext;
            _syncJobService = syncJobService;
        }

        public async Task<List<VpnPeer>> GetByNodeIdAsync(Guid vpnNodeId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.VpnPeers
                .AsNoTracking()
                .Where(x => x.VpnNodeId == vpnNodeId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<VpnPeer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.VpnPeers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<VpnPeer> CreateAsync(VpnPeer peer, CancellationToken cancellationToken = default)
        {
            peer.Id = Guid.NewGuid();

            _dbContext.VpnPeers.Add(peer);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _syncJobService.EnqueueNodeSyncAsync(
                peer.VpnNodeId,
                "PeerCreated",
                JsonSerializer.Serialize(new
                {
                    peerId = peer.Id,
                    peerName = peer.Name
                }),
                cancellationToken);

            return peer;
        }

        public async Task<VpnPeer> UpdateAsync(Guid id, VpnPeer peer, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.VpnPeers
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"VpnPeer not found: {id}");
            }

            existing.Name = peer.Name;
            existing.PublicKey = peer.PublicKey;
            existing.AssignedIp = peer.AssignedIp;
            existing.IsActive = peer.IsActive;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _syncJobService.EnqueueNodeSyncAsync(
                existing.VpnNodeId,
                "PeerUpdated",
                JsonSerializer.Serialize(new
                {
                    peerId = existing.Id,
                    peerName = existing.Name
                }),
                cancellationToken);

            return existing;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.VpnPeers
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (existing == null)
            {
                return;
            }

            var nodeId = existing.VpnNodeId;
            var peerName = existing.Name;

            _dbContext.VpnPeers.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _syncJobService.EnqueueNodeSyncAsync(
                nodeId,
                "PeerDeleted",
                JsonSerializer.Serialize(new
                {
                    peerId = id,
                    peerName = peerName
                }),
                cancellationToken);
        }
    }
}
