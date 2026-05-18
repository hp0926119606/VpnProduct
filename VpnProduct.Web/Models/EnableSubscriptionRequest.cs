namespace VpnProduct.Web.Models;

public class EnableSubscriptionRequest
{
    public string UserEmail { get; set; } = string.Empty;

    public int Days { get; set; } = 30;
}