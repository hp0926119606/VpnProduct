namespace VpnProduct.Application.Models.VpnPeers
{
    public class GetVpnPeerConfigResponse
    {
        public Guid PeerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ClientConfig { get; set; } = string.Empty;
    }
}
