using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VpnProduct.Infrastructure.Data;
using VpnProduct.Web.Models;

namespace VpnProduct.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AuthController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!valid)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "Password invalid"
            });
        }

        var peer = _db.VpnPeers.FirstOrDefault(x => x.Name == user.Email);

        return Ok(new LoginResponse
        {
            Success = true,
            Message = peer == null ? "Login OK, no peer assigned yet." : "OK",
            PeerId = peer == null ? string.Empty : peer.Id.ToString()
        });
    }
}
