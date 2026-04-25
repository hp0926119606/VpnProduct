using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Application.Interfaces;
using VpnProduct.Application.Models.Agent;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Web.Controllers
{
    [ApiController]
    [Route("api/agent/nodes/{nodeId:guid}")]
    public class AgentController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWireGuardServerConfigService _wireGuardServerConfigService;
        private readonly ISyncJobService _syncJobService;

        public AgentController(
            ApplicationDbContext dbContext,
            IWireGuardServerConfigService wireGuardServerConfigService,
            ISyncJobService syncJobService)
        {
            _dbContext = dbContext;
            _wireGuardServerConfigService = wireGuardServerConfigService;
            _syncJobService = syncJobService;
        }

        [HttpGet("sync")]
        public async Task<ActionResult<AgentNodeSyncResponse>> GetSync([FromRoute] Guid nodeId, CancellationToken cancellationToken)
        {
            var node = await ValidateAgentAsync(nodeId, cancellationToken);
            if (node == null)
            {
                return Unauthorized();
            }

            var configText = await _wireGuardServerConfigService.BuildServerConfigAsync(nodeId, cancellationToken);

            var hasPendingJob = await _dbContext.SyncJobs
                .AsNoTracking()
                .AnyAsync(x => x.VpnNodeId == nodeId && x.Status == "Pending", cancellationToken);

            return Ok(new AgentNodeSyncResponse
            {
                NodeId = node.Id,
                NodeName = node.Name,
                InterfaceName = node.InterfaceName,
                ConfigVersion = node.ConfigVersion,
                HasPendingJob = hasPendingJob,
                ConfigText = configText
            });
        }

        [HttpPost("sync-result")]
        public async Task<IActionResult> PostSyncResult([FromRoute] Guid nodeId, [FromBody] AgentSyncResultRequest request, CancellationToken cancellationToken)
        {
            var node = await ValidateAgentAsync(nodeId, cancellationToken);
            if (node == null)
            {
                return Unauthorized();
            }

            await _syncJobService.MarkLatestPendingJobResultAsync(
                nodeId,
                request.ConfigVersion,
                request.Success,
                request.Message,
                cancellationToken);

            node.LastHeartbeatAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }

        private async Task<Domain.Entities.VpnNode?> ValidateAgentAsync(Guid nodeId, CancellationToken cancellationToken)
        {
            if (!Request.Headers.TryGetValue("X-Agent-Token", out var tokenValues))
            {
                return null;
            }

            var token = tokenValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return await _dbContext.VpnNodes
                .FirstOrDefaultAsync(x => x.Id == nodeId && x.AgentToken == token && x.IsActive, cancellationToken);
        }
    }
}
