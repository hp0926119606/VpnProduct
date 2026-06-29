namespace VpnProduct.Domain.Entities;

public class UserDevice
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string DeviceType { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public Guid? VpnPeerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastSeenAtUtc { get; set; }

    public VpnPeer? VpnPeer { get; set; }
}