using System;

namespace VpnProduct.Application.Models.VpnPeers
{
    public class CreateVpnPeerRequest
    {
        public Guid VpnNodeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string AssignedIp { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
