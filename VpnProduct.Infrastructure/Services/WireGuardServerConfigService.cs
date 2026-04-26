using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Application.Interfaces;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Infrastructure.Services
{
    public class WireGuardServerConfigService : IWireGuardServerConfigService
    {
        private readonly ApplicationDbContext _dbContext;

        public WireGuardServerConfigService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> BuildServerConfigAsync(Guid nodeId, CancellationToken cancellationToken = default)
        {
            var node = await _dbContext.VpnNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);

            if (node == null)
            {
                throw new InvalidOperationException($"VpnNode not found: {nodeId}");
            }

            var peers = await _dbContext.VpnPeers
                .AsNoTracking()
                .Where(x => x.VpnNodeId == nodeId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("[Interface]");
            sb.AppendLine($"Address = {node.ServerAddressCidr}");
            sb.AppendLine($"ListenPort = {node.ListenPort}");
		sb.AppendLine();

            foreach (var peer in peers)
            {
                if (string.IsNullOrWhiteSpace(peer.PublicKey))
                {
                    continue;
                }

                sb.AppendLine("[Peer]");
                sb.AppendLine($"# {peer.Name}");
                sb.AppendLine($"PublicKey = {peer.PublicKey}");
                sb.AppendLine($"AllowedIPs = {NormalizeToSingleHostCidr(peer.AssignedIp)}");
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private static string NormalizeToSingleHostCidr(string assignedIp)
        {
            if (assignedIp.Contains('/'))
            {
                return assignedIp;
            }

            return $"{assignedIp}/32";
        }
    }
}
