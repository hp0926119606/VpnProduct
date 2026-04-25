using System;

namespace VpnProduct.Agent.Models
{
    public class AgentNodeSyncResponse
    {
        public Guid NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public long ConfigVersion { get; set; }
        public bool HasPendingJob { get; set; }
        public string ConfigText { get; set; } = string.Empty;
    }
}
