namespace VpnProduct.Web.Models;

public class LoginRequest
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string DeviceType { get; set; } = "";

    public string DeviceIdentifier { get; set; } = "";
}