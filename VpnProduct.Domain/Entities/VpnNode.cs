using System;
using System.Collections.Generic;

namespace VpnProduct.Domain.Entities
{
    public class VpnNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = "wg0";
        public string ServerAddressCidr { get; set; } = string.Empty;
        public int ListenPort { get; set; } = 51820;
        public string AgentToken { get; set; } = Guid.NewGuid().ToString("N");
        public long ConfigVersion { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime? LastHeartbeatAtUtc { get; set; }

        public ICollection<VpnPeer> Peers { get; set; } = new List<VpnPeer>();
        public ICollection<SyncJob> SyncJobs { get; set; } = new List<SyncJob>();
    }
}
