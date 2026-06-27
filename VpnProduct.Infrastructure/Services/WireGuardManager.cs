using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnProduct.Application.Interfaces;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Infrastructure.Services;

public sealed class WireGuardManager : IWireGuardManager
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public WireGuardManager(
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task ApplyConfigurationAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var configPath =
            _configuration["VpnProduct:ServerWireGuardConfigPath"] ??
            "/etc/wireguard/wg1.conf";

        var interfaceName =
            Path.GetFileNameWithoutExtension(configPath);

        var syncPath =
            $"/etc/wireguard/{interfaceName}.sync.conf";

        var node = await _db.VpnNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == nodeId && x.IsActive,
                cancellationToken);

        if (node == null)
        {
            throw new InvalidOperationException(
                $"Active VpnNode not found: {nodeId}");
        }

        var peers = await _db.VpnPeers
            .AsNoTracking()
            .Where(x => x.VpnNodeId == nodeId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var privateKey = ReadPrivateKey(configPath);

        var fullConfig = BuildFullWgQuickConfig(
            privateKey,
            node.ServerAddressCidr,
            node.ListenPort,
            peers);

        var syncConfig = BuildSyncConfig(
            privateKey,
            node.ListenPort,
            peers);

        BackupFile(configPath);

        await File.WriteAllTextAsync(
            configPath,
            fullConfig,
            new UTF8Encoding(false),
            cancellationToken);

        await File.WriteAllTextAsync(
            syncPath,
            syncConfig,
            new UTF8Encoding(false),
            cancellationToken);

        RunCommand("/usr/bin/chmod", $"600 \"{syncPath}\"");

        RunCommand(
            "/usr/bin/wg",
            $"syncconf {interfaceName} \"{syncPath}\"");
    }

    private static string BuildFullWgQuickConfig(
        string privateKey,
        string serverAddressCidr,
        int listenPort,
        IReadOnlyCollection<Domain.Entities.VpnPeer> peers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {privateKey}");
        sb.AppendLine($"Address = {serverAddressCidr}");
        sb.AppendLine($"ListenPort = {listenPort}");
        sb.AppendLine("PostUp = iptables -A FORWARD -i %i -j ACCEPT");
        sb.AppendLine("PostUp = iptables -A FORWARD -o %i -j ACCEPT");
        sb.AppendLine("PostUp = iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE");
        sb.AppendLine("PostDown = iptables -D FORWARD -i %i -j ACCEPT");
        sb.AppendLine("PostDown = iptables -D FORWARD -o %i -j ACCEPT");
        sb.AppendLine("PostDown = iptables -t nat -D POSTROUTING -o eth0 -j MASQUERADE");
        sb.AppendLine();

        AppendPeers(sb, peers);

        return sb.ToString();
    }

    private static string BuildSyncConfig(
        string privateKey,
        int listenPort,
        IReadOnlyCollection<Domain.Entities.VpnPeer> peers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {privateKey}");
        sb.AppendLine($"ListenPort = {listenPort}");
        sb.AppendLine();

        AppendPeers(sb, peers);

        return sb.ToString();
    }

    private static void AppendPeers(
        StringBuilder sb,
        IReadOnlyCollection<Domain.Entities.VpnPeer> peers)
    {
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
    }

    private static string ReadPrivateKey(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                $"WireGuard config not found: {configPath}");
        }

        var privateKeyLine = File.ReadAllLines(configPath)
            .FirstOrDefault(x =>
                x.TrimStart().StartsWith(
                    "PrivateKey",
                    StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(privateKeyLine))
        {
            throw new InvalidOperationException(
                $"PrivateKey not found in {configPath}");
        }

        return privateKeyLine.Split('=', 2)[1].Trim();
    }

    private static string NormalizeToSingleHostCidr(string assignedIp)
    {
        if (string.IsNullOrWhiteSpace(assignedIp))
        {
            throw new InvalidOperationException("AssignedIp is empty.");
        }

        var value = assignedIp.Trim();

        if (value.Contains('/'))
        {
            return value;
        }

        return $"{value}/32";
    }

    private static void BackupFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var backupDir = Path.Combine(
            Path.GetDirectoryName(path) ?? "/etc/wireguard",
            "backup");

        Directory.CreateDirectory(backupDir);

        var backupPath = Path.Combine(
            backupDir,
            $"{Path.GetFileName(path)}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak");

        File.Copy(path, backupPath, overwrite: false);
    }

    private static string RunCommand(
        string fileName,
        string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process =
            Process.Start(psi) ??
            throw new InvalidOperationException(
                $"Failed to start {fileName}");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} {arguments} failed: {error}");
        }

        return output;
    }
}