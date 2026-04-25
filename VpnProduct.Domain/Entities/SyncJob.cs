using System;

namespace VpnProduct.Domain.Entities
{
    public class SyncJob
    {
        public Guid Id { get; set; }
        public Guid VpnNodeId { get; set; }
        public string Status { get; set; } = "Pending";
        public string JobType { get; set; } = string.Empty;
        public string? PayloadJson { get; set; }
        public long ConfigVersion { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAtUtc { get; set; }
        public string? ResultMessage { get; set; }
    }
}
