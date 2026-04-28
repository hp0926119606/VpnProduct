namespace VpnProduct.Application.Models.VpnPeers
{
    public class CreateVpnPeerRequest
    {
        public Guid VpnNodeId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
