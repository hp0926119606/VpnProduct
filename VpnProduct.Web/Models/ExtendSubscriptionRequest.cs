namespace VpnProduct.Web.Models;

public class ExtendSubscriptionRequest
{
    public string UserEmail { get; set; } = string.Empty;

    public int Days { get; set; }
}