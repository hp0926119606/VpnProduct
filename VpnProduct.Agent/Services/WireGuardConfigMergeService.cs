using VpnProduct.Agent.Models;

namespace VpnProduct.Agent.Services
{
    public class WireGuardConfigMergeService
    {
        private readonly AgentOptions _options;

        public WireGuardConfigMergeService(AgentOptions options)
        {
            _options = options;
        }

        public async Task<string> BuildMergedConfigAsync(string webConfigText, CancellationToken cancellationToken = default)
        {
            var localConfigPath = _options.LocalWireGuardConfigPath;
            var mergedConfigPath = _options.ConfigOutputPath;

            if (string.IsNullOrWhiteSpace(localConfigPath))
            {
                throw new InvalidOperationException("LocalWireGuardConfigPath is empty.");
            }

            if (!File.Exists(localConfigPath))
            {
                throw new FileNotFoundException($"Local WireGuard config not found: {localConfigPath}");
            }

            var localConfigText = await File.ReadAllTextAsync(localConfigPath, cancellationToken);

            var localInterfaceBlock = ExtractInterfaceBlock(localConfigText);
            if (string.IsNullOrWhiteSpace(localInterfaceBlock))
            {
                throw new InvalidOperationException($"No [Interface] block found in local config: {localConfigPath}");
            }

            var peerBlocks = ExtractPeerBlocks(webConfigText);

            var mergedText = localInterfaceBlock.TrimEnd();

            if (peerBlocks.Count > 0)
            {
                mergedText += Environment.NewLine + Environment.NewLine;
                mergedText += string.Join(Environment.NewLine + Environment.NewLine, peerBlocks.Select(x => x.Trim()));
            }

            var directory = Path.GetDirectoryName(mergedConfigPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(mergedConfigPath, mergedText.TrimEnd() + Environment.NewLine, cancellationToken);

            return mergedConfigPath;
        }

        private static string ExtractInterfaceBlock(string configText)
        {
            var lines = SplitLines(configText);
            var result = new List<string>();
            var inInterface = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Equals("[Interface]", StringComparison.OrdinalIgnoreCase))
                {
                    inInterface = true;
                    result.Add(line);
                    continue;
                }

                if (inInterface && trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    break;
                }

                if (inInterface)
                {
                    result.Add(line);
                }
            }

            return string.Join(Environment.NewLine, result).Trim();
        }

        private static List<string> ExtractPeerBlocks(string configText)
        {
            var lines = SplitLines(configText);
            var result = new List<string>();

            List<string>? currentPeer = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Equals("[Peer]", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentPeer != null && currentPeer.Count > 0)
                    {
                        result.Add(string.Join(Environment.NewLine, currentPeer).Trim());
                    }

                    currentPeer = new List<string> { line };
                    continue;
                }

                if (currentPeer != null)
                {
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        if (currentPeer.Count > 0)
                        {
                            result.Add(string.Join(Environment.NewLine, currentPeer).Trim());
                        }

                        currentPeer = null;
                        continue;
                    }

                    currentPeer.Add(line);
                }
            }

            if (currentPeer != null && currentPeer.Count > 0)
            {
                result.Add(string.Join(Environment.NewLine, currentPeer).Trim());
            }

            return result;
        }

        private static string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        }
    }
}
