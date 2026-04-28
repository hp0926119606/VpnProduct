namespace VpnProduct.Application.Models.VpnPeers
{
    public class CreateVpnPeerResponse
    {
        public Guid Id { get; set; }
        public Guid VpnNodeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string AssignedIp { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public long ConfigVersion { get; set; }
        public string ClientConfig { get; set; } = string.Empty;
    }
}
