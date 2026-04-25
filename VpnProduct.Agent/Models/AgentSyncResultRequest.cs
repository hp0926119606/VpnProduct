namespace VpnProduct.Agent.Models
{
    public class AgentSyncResultRequest
    {
        public long ConfigVersion { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
