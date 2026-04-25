using System.Diagnostics;
using VpnProduct.Agent.Models;

namespace VpnProduct.Agent.Services
{
    public class WireGuardApplyService
    {
        private readonly AgentOptions _options;
        private readonly ILogger<WireGuardApplyService> _logger;

        public WireGuardApplyService(AgentOptions options, ILogger<WireGuardApplyService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> ApplyAsync(CancellationToken cancellationToken = default)
        {
            if (_options.SimulateApply)
            {
                _logger.LogInformation("SimulateApply=true, skip real wg syncconf.");
                return (true, "Simulated apply success");
            }

            if (string.IsNullOrWhiteSpace(_options.ApplyCommand))
            {
                return (false, "ApplyCommand is empty");
            }

            var arguments = (_options.ApplyArgumentsTemplate ?? string.Empty)
                .Replace("{interfaceName}", _options.InterfaceName)
                .Replace("{configPath}", _options.ConfigOutputPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.ApplyCommand,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            if (process.ExitCode == 0)
            {
                return (true, string.IsNullOrWhiteSpace(stdOut) ? "Apply success" : stdOut.Trim());
            }

            var errorMessage = string.IsNullOrWhiteSpace(stdErr) ? $"Apply failed with exit code {process.ExitCode}" : stdErr.Trim();
            return (false, errorMessage);
        }
    }
}
