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
    private readonly IWireGuardManager _wireGuardManager;

    public AuthController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        IVpnPeerService vpnPeerService,
        IEmailSender emailSender,
        IConfiguration configuration,
        IWireGuardManager wireGuardManager)
    {
        _userManager = userManager;
        _db = db;
        _vpnPeerService = vpnPeerService;
        _emailSender = emailSender;
        _configuration = configuration;
        _wireGuardManager = wireGuardManager;
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

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        var publicBaseUrl =
            _configuration["VpnProduct:PublicBaseUrl"] ??
            "https://yct.myftp.org:8443";

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
            PeerId = ""
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
            return LoginFail("User not found");
        }

        if (!user.EmailConfirmed)
        {
            return LoginFail("Email not confirmed");
        }

        var valid =
            await _userManager.CheckPasswordAsync(user, request.Password);

        if (!valid)
        {
            return LoginFail("Password invalid");
        }

        var subscription = await _db.Subscriptions
            .Where(x => x.UserEmail == email)
            .OrderByDescending(x => x.ExpireAtUtc)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return LoginFail("Subscription not found");
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
                deviceId = "",
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
                deviceId = "",
                subscriptionActive = false,
                expireAtUtc = subscription.ExpireAtUtc,
                daysRemaining = 0
            });
        }

        var deviceName =
            string.IsNullOrWhiteSpace(request.DeviceName)
                ? "Windows Device"
                : request.DeviceName.Trim();

        var deviceType =
            string.IsNullOrWhiteSpace(request.DeviceType)
                ? "Windows"
                : request.DeviceType.Trim();

        var deviceIdentifier =
            string.IsNullOrWhiteSpace(request.DeviceIdentifier)
                ? $"{email}:{deviceName}"
                : request.DeviceIdentifier.Trim();

        var device = await _db.UserDevices
            .Include(x => x.VpnPeer)
            .FirstOrDefaultAsync(x =>
                x.UserId == user.Id &&
                x.DeviceIdentifier == deviceIdentifier);

        var changedWireGuard = false;

        if (device == null)
        {
            var peerName =
                $"{email} / {deviceName}";

            var createdPeer =
                await _vpnPeerService.CreateAsync(new CreateVpnPeerRequest
                {
                    VpnNodeId = DefaultVpnNodeId,
                    Name = peerName
                });

            device = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                UserEmail = email,
                DeviceName = deviceName,
                DeviceType = deviceType,
                DeviceIdentifier = deviceIdentifier,
                VpnPeerId = createdPeer.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow
            };

            _db.UserDevices.Add(device);

            await _db.SaveChangesAsync();

            changedWireGuard = true;
        }
        else
        {
            device.UserEmail = email;
            device.DeviceName = deviceName;
            device.DeviceType = deviceType;
            device.LastSeenAtUtc = DateTime.UtcNow;

            if (!device.IsActive)
            {
                return Ok(new
                {
                    success = false,
                    message = "Device inactive",
                    peerId = "",
                    deviceId = device.Id.ToString(),
                    subscriptionActive = true,
                    expireAtUtc = subscription.ExpireAtUtc,
                    daysRemaining = Math.Max(daysRemaining, 0)
                });
            }

            if (device.VpnPeerId == null)
            {
                var peerName =
                    $"{email} / {deviceName}";

                var createdPeer =
                    await _vpnPeerService.CreateAsync(new CreateVpnPeerRequest
                    {
                        VpnNodeId = DefaultVpnNodeId,
                        Name = peerName
                    });

                device.VpnPeerId = createdPeer.Id;
                changedWireGuard = true;
            }

            await _db.SaveChangesAsync();
        }

        if (changedWireGuard)
        {
            await _wireGuardManager.ApplyConfigurationAsync(DefaultVpnNodeId);
        }

        return Ok(new
        {
            success = true,
            message = "OK",
            peerId = device.VpnPeerId?.ToString() ?? "",
            deviceId = device.Id.ToString(),
            subscriptionActive = true,
            expireAtUtc = subscription.ExpireAtUtc,
            daysRemaining = Math.Max(daysRemaining, 0)
        });
    }

    private static IActionResult LoginFail(string message)
    {
        return new OkObjectResult(new
        {
            success = false,
            message,
            peerId = "",
            deviceId = "",
            subscriptionActive = false,
            expireAtUtc = (DateTime?)null,
            daysRemaining = 0
        });
    }
}