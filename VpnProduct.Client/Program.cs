using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

Console.WriteLine("=== VPN Client ===");

Console.Write("API URL (例如 http://61.70.3.87:5049): ");
var baseUrl = Console.ReadLine();

Console.Write("PeerId: ");
var peerId = Console.ReadLine();

if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(peerId))
{
    Console.WriteLine("Input invalid.");
    return;
}

var client = new HttpClient();

var url = $"{baseUrl}/api/vpnpeers/{peerId}/config";

Console.WriteLine("Downloading config...");

var response = await client.GetStringAsync(url);

using var doc = JsonDocument.Parse(response);

var config = doc.RootElement.GetProperty("clientConfig").GetString();

if (string.IsNullOrWhiteSpace(config))
{
    Console.WriteLine("Config empty.");
    return;
}

var path = @"C:\ProgramData\VpnProduct\wg0.conf";

Directory.CreateDirectory(@"C:\ProgramData\VpnProduct");

await File.WriteAllTextAsync(path, config);

Console.WriteLine("Config saved: " + path);

// ⚠️ WireGuard CLI（需已安裝 WireGuard）
var wireguardExe = @"C:\Program Files\WireGuard\wireguard.exe";

if (!File.Exists(wireguardExe))
{
    Console.WriteLine("WireGuard not installed.");
    return;
}

Console.WriteLine("Starting tunnel...");

var psi = new ProcessStartInfo
{
    FileName = wireguardExe,
    Arguments = $" /installtunnelservice \"{path}\"",
    UseShellExecute = true
};

Process.Start(psi);

Console.WriteLine("Done.");