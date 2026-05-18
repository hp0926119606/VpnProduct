using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Application.Interfaces;
using VpnProduct.Application.Models.VpnPeers;
using VpnProduct.Domain.Entities;
using VpnProduct.Infrastructure.Data;
using VpnProduct.Web.Models;

namespace VpnProduct.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly Guid DefaultVpnNodeId =
        Guid.Parse("d5f17451-8a40-41ac-88f5-3ccdeca86f0a");

    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IVpnPeerService _vpnPeerService;

    public AuthController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        IVpnPeerService vpnPeerService)
    {
        _userManager = userManager;
        _db = db;
        _vpnPeerService = vpnPeerService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Ok(new RegisterResponse
            {
                Success = false,
                Message = "Email and password are required."
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return Ok(new RegisterResponse
            {
                Success = false,
                Message = "Email already registered."
            });
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createUserResult =
            await _userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            return Ok(new RegisterResponse
            {
                Success = false,
                Message = string.Join("; ", createUserResult.Errors.Select(x => x.Description))
            });
        }

        _db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            UserEmail = email,
            StartAtUtc = DateTime.UtcNow,
            ExpireAtUtc = DateTime.UtcNow.AddDays(7),
            IsActive = true
        });

        await _db.SaveChangesAsync();

        var createdPeer = await _vpnPeerService.CreateAsync(new CreateVpnPeerRequest
        {
            VpnNodeId = DefaultVpnNodeId,
            Name = email
        });

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "OK",
            PeerId = createdPeer.Id.ToString()
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        var valid =
            await _userManager.CheckPasswordAsync(user, request.Password);

        if (!valid)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "Password invalid"
            });
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(x =>
                x.UserEmail == email &&
                x.IsActive);

        if (subscription == null)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "Subscription not found"
            });
        }

        if (subscription.ExpireAtUtc <= DateTime.UtcNow)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Message = "Subscription expired"
            });
        }

        var existingPeer = await _db.VpnPeers
            .Where(x => x.Name == email && x.IsActive)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (existingPeer != null)
        {
            return Ok(new LoginResponse
            {
                Success = true,
                Message = "OK",
                PeerId = existingPeer.Id.ToString()
            });
        }

        var createdPeer = await _vpnPeerService.CreateAsync(new CreateVpnPeerRequest
        {
            VpnNodeId = DefaultVpnNodeId,
            Name = email
        });

        return Ok(new LoginResponse
        {
            Success = true,
            Message = "OK",
            PeerId = createdPeer.Id.ToString()
        });
    }
}