using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Application.Interfaces;
using VpnProduct.Domain.Entities;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Infrastructure.Services
{
    public class SyncJobService : ISyncJobService
    {
        private readonly ApplicationDbContext _dbContext;

        public SyncJobService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SyncJob> EnqueueNodeSyncAsync(Guid vpnNodeId, string jobType, string? payloadJson = null, CancellationToken cancellationToken = default)
        {
            var node = await _dbContext.VpnNodes.FirstOrDefaultAsync(x => x.Id == vpnNodeId, cancellationToken);

            if (node == null)
            {
                throw new InvalidOperationException($"VpnNode not found: {vpnNodeId}");
            }

            node.ConfigVersion += 1;

            var job = new SyncJob
            {
                Id = Guid.NewGuid(),
                VpnNodeId = vpnNodeId,
                Status = "Pending",
                JobType = jobType,
                PayloadJson = payloadJson,
                ConfigVersion = node.ConfigVersion,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.SyncJobs.Add(job);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return job;
        }

        public async Task MarkLatestPendingJobResultAsync(Guid vpnNodeId, long configVersion, bool success, string? message, CancellationToken cancellationToken = default)
        {
            var job = await _dbContext.SyncJobs
                .Where(x => x.VpnNodeId == vpnNodeId && x.ConfigVersion == configVersion && x.Status == "Pending")
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (job == null)
            {
                return;
            }

            job.FinishedAtUtc = DateTime.UtcNow;
            job.Status = success ? "Succeeded" : "Failed";
            job.ResultMessage = message;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
