using System.Text;
using VpnProduct.Agent.Models;

namespace VpnProduct.Agent.Services
{
    public class ConfigFileService
    {
        private readonly AgentOptions _options;

        public ConfigFileService(AgentOptions options)
        {
            _options = options;
        }

        public async Task WriteConfigAsync(string configText, CancellationToken cancellationToken = default)
        {
            var filePath = _options.ConfigOutputPath;
            string? directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, configText, new UTF8Encoding(false), cancellationToken);
        }
    }
}
