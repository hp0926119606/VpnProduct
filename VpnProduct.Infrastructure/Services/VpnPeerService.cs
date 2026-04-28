using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnProduct.Application.Interfaces;
using VpnProduct.Application.Models.VpnPeers;
using VpnProduct.Domain.Entities;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Infrastructure.Services
{
    public class VpnPeerService : IVpnPeerService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public VpnPeerService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<CreateVpnPeerResponse> CreateAsync(CreateVpnPeerRequest request, CancellationToken cancellationToken = default)
        {
            var node = await _db.VpnNodes
                .FirstOrDefaultAsync(x => x.Id == request.VpnNodeId && x.IsActive, cancellationToken);

            if (node == null)
            {
                throw new InvalidOperationException("VpnNode not found or inactive.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Peer name is required.");
            }

            var keyPair = GenerateWireGuardKeyPair();
            var assignedIp = await AllocateNextIpAsync(node.Id, cancellationToken);

            var peer = new VpnPeer
            {
                Id = Guid.NewGuid(),
                VpnNodeId = node.Id,
                Name = request.Name.Trim(),
                PublicKey = keyPair.PublicKey,
                AssignedIp = assignedIp,
                IsActive = true
            };

            node.ConfigVersion += 1;

            _db.VpnPeers.Add(peer);

            _db.SyncJobs.Add(new SyncJob
            {
                Id = Guid.NewGuid(),
                VpnNodeId = node.Id,
                Status = "Pending",
                JobType = "PeerCreated",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    peer.Id,
                    peer.Name,
                    peer.PublicKey,
                    peer.AssignedIp
                }),
                ConfigVersion = node.ConfigVersion,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            var clientConfig = BuildClientConfig(node, keyPair.PrivateKey, assignedIp);

            return new CreateVpnPeerResponse
            {
                Id = peer.Id,
                VpnNodeId = peer.VpnNodeId,
                Name = peer.Name,
                PublicKey = peer.PublicKey,
                AssignedIp = peer.AssignedIp,
                IsActive = peer.IsActive,
                ConfigVersion = node.ConfigVersion,
                ClientConfig = clientConfig
            };
        }

        private async Task<string> AllocateNextIpAsync(Guid nodeId, CancellationToken cancellationToken)
        {
            var prefix = _configuration["VpnProduct:PeerIpPrefix"] ?? "10.0.1.";
            var startText = _configuration["VpnProduct:PeerIpStart"] ?? "201";
            var endText = _configuration["VpnProduct:PeerIpEnd"] ?? "254";

            var start = int.Parse(startText);
            var end = int.Parse(endText);

            var usedIps = await _db.VpnPeers
                .Where(x => x.VpnNodeId == nodeId)
                .Select(x => x.AssignedIp)
                .ToListAsync(cancellationToken);

            for (var i = start; i <= end; i++)
            {
                var ip = $"{prefix}{i}/32";
                var ipWithoutCidr = $"{prefix}{i}";

                if (!usedIps.Contains(ip) && !usedIps.Contains(ipWithoutCidr))
                {
                    return ip;
                }
            }

            throw new InvalidOperationException("No available VPN client IP.");
        }

        private WireGuardKeyPair GenerateWireGuardKeyPair()
        {
            var privateKey = RunCommand("/bin/bash", "-c \"wg genkey\"").Trim();
            var publicKey = RunCommandWithStandardInput("/usr/bin/wg", "pubkey", privateKey).Trim();

            if (string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(publicKey))
            {
                throw new InvalidOperationException("Failed to generate WireGuard key pair.");
            }

            return new WireGuardKeyPair(privateKey, publicKey);
        }

        private string BuildClientConfig(VpnNode node, string clientPrivateKey, string assignedIp)
        {
            var endpointHost = _configuration["VpnProduct:EndpointHost"] ?? "61.70.3.87";
            var dns = _configuration["VpnProduct:ClientDns"] ?? "8.8.8.8";
            var allowedIps = _configuration["VpnProduct:ClientAllowedIps"] ?? "0.0.0.0/0";
            var serverPublicKey = GetServerPublicKey();

            return $"""
[Interface]
PrivateKey = {clientPrivateKey}
Address = {assignedIp}
DNS = {dns}

[Peer]
PublicKey = {serverPublicKey}
Endpoint = {endpointHost}:{node.ListenPort}
AllowedIPs = {allowedIps}
PersistentKeepalive = 25
""";
        }

        private string GetServerPublicKey()
        {
            var configured = _configuration["VpnProduct:ServerPublicKey"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }

            var serverConfigPath = _configuration["VpnProduct:ServerWireGuardConfigPath"] ?? "/etc/wireguard/wg1.conf";

            if (!File.Exists(serverConfigPath))
            {
                throw new FileNotFoundException($"Server WireGuard config not found: {serverConfigPath}");
            }

            var privateKeyLine = File.ReadAllLines(serverConfigPath)
                .FirstOrDefault(x => x.TrimStart().StartsWith("PrivateKey", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(privateKeyLine))
            {
                throw new InvalidOperationException($"PrivateKey not found in {serverConfigPath}");
            }

            var privateKey = privateKeyLine.Split('=', 2)[1].Trim();

            return RunCommandWithStandardInput("/usr/bin/wg", "pubkey", privateKey).Trim();
        }

        private static string RunCommand(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName}");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{fileName} failed: {error}");
            }

            return output;
        }

        private static string RunCommandWithStandardInput(string fileName, string arguments, string standardInput)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName}");

            process.StandardInput.WriteLine(standardInput);
            process.StandardInput.Close();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{fileName} failed: {error}");
            }

            return output;
        }

        private sealed record WireGuardKeyPair(string PrivateKey, string PublicKey);
    }
}
