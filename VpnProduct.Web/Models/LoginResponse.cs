namespace VpnProduct.Web.Models;

public class LoginResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string PeerId { get; set; } = string.Empty;
}