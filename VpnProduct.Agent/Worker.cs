using VpnProduct.Agent.Models;
using VpnProduct.Agent.Services;

namespace VpnProduct.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AgentOptions _options;
        private readonly AgentApiClient _apiClient;
        private readonly WireGuardConfigMergeService _mergeService;
        private readonly WireGuardApplyService _applyService;

        private long _lastAppliedConfigVersion = 0;

        public Worker(
            ILogger<Worker> logger,
            AgentOptions options,
            AgentApiClient apiClient,
            WireGuardConfigMergeService mergeService,
            WireGuardApplyService applyService)
        {
            _logger = logger;
            _options = options;
            _apiClient = apiClient;
            _mergeService = mergeService;
            _applyService = applyService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VpnProduct.Agent started. NodeId={NodeId}, Interface={InterfaceName}", _options.NodeId, _options.InterfaceName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var syncResponse = await _apiClient.GetSyncAsync(stoppingToken);

                    if (syncResponse == null)
                    {
                        _logger.LogWarning("Sync response is null.");
                    }
                    else
                    {
                        var shouldApply =
                            syncResponse.HasPendingJob ||
                            syncResponse.ConfigVersion > _lastAppliedConfigVersion;

                        if (shouldApply)
                        {
                            _logger.LogInformation(
                                "Merging config. Version={Version}, Pending={Pending}",
                                syncResponse.ConfigVersion,
                                syncResponse.HasPendingJob);

                            var mergedPath = await _mergeService.BuildMergedConfigAsync(syncResponse.ConfigText, stoppingToken);

                            _logger.LogInformation("Merged config written to {MergedPath}", mergedPath);

                            var applyResult = await _applyService.ApplyAsync(stoppingToken);

                            await _apiClient.PostSyncResultAsync(
                                new AgentSyncResultRequest
                                {
                                    ConfigVersion = syncResponse.ConfigVersion,
                                    Success = applyResult.Success,
                                    Message = applyResult.Message
                                },
                                stoppingToken);

                            if (applyResult.Success)
                            {
                                _lastAppliedConfigVersion = syncResponse.ConfigVersion;
                                _logger.LogInformation("Apply success. Version={Version}", syncResponse.ConfigVersion);
                            }
                            else
                            {
                                _logger.LogError("Apply failed. Version={Version}, Message={Message}",
                                    syncResponse.ConfigVersion,
                                    applyResult.Message);
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No apply needed. Version={Version}, LastApplied={LastApplied}",
                                syncResponse.ConfigVersion,
                                _lastAppliedConfigVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
        }
    }
}
