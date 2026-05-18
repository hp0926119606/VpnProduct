namespace VpnProduct.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public DateTime StartAtUtc { get; set; }

    public DateTime ExpireAtUtc { get; set; }

    public bool IsActive { get; set; }
}