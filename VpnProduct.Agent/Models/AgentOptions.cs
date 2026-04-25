namespace VpnProduct.Agent.Models
{
    public class AgentOptions
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:5049";
        public string NodeId { get; set; } = string.Empty;
        public string AgentToken { get; set; } = string.Empty;
        public int PollIntervalSeconds { get; set; } = 10;

        public string InterfaceName { get; set; } = "wg1";

        // 本機真實 WireGuard 設定檔，保留 [Interface] 和 PrivateKey
        public string LocalWireGuardConfigPath { get; set; } = "/etc/wireguard/wg1.conf";

        // Agent 合併後輸出的暫存檔，wg syncconf 會套用這個
        public string ConfigOutputPath { get; set; } = "/tmp/vpnproduct-wg1-merged.conf";

        public bool SimulateApply { get; set; } = true;

        public string ApplyCommand { get; set; } = "/usr/bin/wg";

        public string ApplyArgumentsTemplate { get; set; } = "syncconf {interfaceName} {configPath}";
    }
}
