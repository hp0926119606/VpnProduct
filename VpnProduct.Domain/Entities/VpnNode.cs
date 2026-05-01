namespace VpnProduct.Domain.Entities
{
    public class VpnNode
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ServerAddressCidr { get; set; } = string.Empty;
        public string AgentToken { get; set; } = string.Empty;

        public int ListenPort { get; set; } = 51820;
        public long ConfigVersion { get; set; } = 1;

        public bool IsActive { get; set; } = true;
        public DateTime? LastHeartbeatAtUtc { get; set; }

        public ICollection<VpnPeer> Peers { get; set; } = new List<VpnPeer>();
    }
}
