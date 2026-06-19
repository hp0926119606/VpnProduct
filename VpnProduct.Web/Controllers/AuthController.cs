using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

using System.Text;

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
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        IVpnPeerService vpnPeerService,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _db = db;
        _vpnPeerService = vpnPeerService;
        _emailSender = emailSender;
        _configuration = configuration;
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
            EmailConfirmed = false
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

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        var publicBaseUrl =
            _configuration["VpnProduct:PublicBaseUrl"] ??
            "https://yct.myftp.org";

        var confirmUrl =
            $"{publicBaseUrl.TrimEnd('/')}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        var html =
            $"""
            <h2>VpnProduct Email Confirmation</h2>
            <p>請點擊以下連結完成 Email 驗證：</p>
            <p><a href="{confirmUrl}">確認 Email</a></p>
            <p>如果你沒有註冊 VpnProduct，請忽略此信。</p>
            """;

        await _emailSender.SendAsync(
            email,
            "VpnProduct Email Confirmation",
            html);

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Registration successful. Please check your email to confirm your account.",
            PeerId = createdPeer.Id.ToString()
        });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string token)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Content("User not found.");
        }

        var decodedToken = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ConfirmEmailAsync(
            user,
            decodedToken);

        if (!result.Succeeded)
        {
            return Content("Email confirmation failed.");
        }

        return Content("Email confirmed. You can now login to VpnProduct Client.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Ok(new
            {
                success = false,
                message = "User not found",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = (DateTime?)null,
                daysRemaining = 0
            });
        }

        if (!user.EmailConfirmed)
        {
            return Ok(new
            {
                success = false,
                message = "Email not confirmed",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = (DateTime?)null,
                daysRemaining = 0
            });
        }

        var valid =
            await _userManager.CheckPasswordAsync(user, request.Password);

        if (!valid)
        {
            return Ok(new
            {
                success = false,
                message = "Password invalid",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = (DateTime?)null,
                daysRemaining = 0
            });
        }

        var subscription = await _db.Subscriptions
            .Where(x => x.UserEmail == email)
            .OrderByDescending(x => x.ExpireAtUtc)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return Ok(new
            {
                success = false,
                message = "Subscription not found",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = (DateTime?)null,
                daysRemaining = 0
            });
        }

        var now = DateTime.UtcNow;

        var daysRemaining =
            (int)Math.Ceiling((subscription.ExpireAtUtc - now).TotalDays);

        if (!subscription.IsActive)
        {
            return Ok(new
            {
                success = false,
                message = "Subscription inactive",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = subscription.ExpireAtUtc,
                daysRemaining = Math.Max(daysRemaining, 0)
            });
        }

        if (subscription.ExpireAtUtc <= now)
        {
            return Ok(new
            {
                success = false,
                message = "Subscription expired",
                peerId = "",
                subscriptionActive = false,
                expireAtUtc = subscription.ExpireAtUtc,
                daysRemaining = 0
            });
        }

        var existingPeer = await _db.VpnPeers
            .Where(x => x.Name == email && x.IsActive)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (existingPeer != null)
        {
            return Ok(new
            {
                success = true,
                message = "OK",
                peerId = existingPeer.Id.ToString(),
                subscriptionActive = true,
                expireAtUtc = subscription.ExpireAtUtc,
                daysRemaining = Math.Max(daysRemaining, 0)
            });
        }

        var createdPeer = await _vpnPeerService.CreateAsync(new CreateVpnPeerRequest
        {
            VpnNodeId = DefaultVpnNodeId,
            Name = email
        });

        return Ok(new
        {
            success = true,
            message = "OK",
            peerId = createdPeer.Id.ToString(),
            subscriptionActive = true,
            expireAtUtc = subscription.ExpireAtUtc,
            daysRemaining = Math.Max(daysRemaining, 0)
        });
    }
}