using System.Net.Http.Json;
using VpnProduct.Agent.Models;

namespace VpnProduct.Agent.Services
{
    public class AgentApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AgentOptions _options;

        public AgentApiClient(HttpClient httpClient, AgentOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<AgentNodeSyncResponse?> GetSyncAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/agent/nodes/{_options.NodeId}/sync");

            request.Headers.Add("X-Agent-Token", _options.AgentToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AgentNodeSyncResponse>(cancellationToken: cancellationToken);
        }

        public async Task PostSyncResultAsync(AgentSyncResultRequest requestBody, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/agent/nodes/{_options.NodeId}/sync-result");

            request.Headers.Add("X-Agent-Token", _options.AgentToken);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
