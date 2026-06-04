using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Infrastructure.Data;
using VpnProduct.Web.Models;

namespace VpnProduct.Web.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private bool IsAdminAuthorized()
    {
        var token = Request.Headers["X-Admin-Token"].ToString();

        return token == "admin-123456";
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize != 50 && pageSize != 100)
        {
            pageSize = 50;
        }

        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            usersQuery = usersQuery.Where(x =>
                x.Email != null &&
                x.Email.Contains(search));
        }

        var totalCount = await usersQuery.CountAsync();

        var users = await usersQuery
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var emails = users
            .Select(x => x.Email ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var subscriptions = await _db.Subscriptions
            .Where(x => emails.Contains(x.UserEmail))
            .ToListAsync();

        var peers = await _db.VpnPeers
            .Where(x => emails.Contains(x.Name))
            .ToListAsync();

        var data = users.Select(user =>
        {
            var email = user.Email ?? "";

            var sub = subscriptions
                .Where(x => x.UserEmail == email)
                .OrderByDescending(x => x.ExpireAtUtc)
                .FirstOrDefault();

            var peer = peers
                .Where(x => x.Name == email)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            return new
            {
                user.Id,
                Email = email,
                user.UserName,
                user.EmailConfirmed,
                SubscriptionActive = sub?.IsActive ?? false,
                SubscriptionExpireAtUtc = sub?.ExpireAtUtc,
                IsExpired = sub != null && sub.ExpireAtUtc <= DateTime.UtcNow,
                PeerId = peer?.Id,
                AssignedIp = peer?.AssignedIp,
                PublicKey = peer?.PublicKey,
                
                PeerActive = peer?.IsActive ?? false
            };
        });

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            data
        });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;

        var subscriptions = await _db.Subscriptions
            .OrderBy(x => x.ExpireAtUtc)
            .Select(x => new
            {
                x.UserEmail,
                x.StartAtUtc,
                x.ExpireAtUtc,
                x.IsActive,
                IsExpired = x.ExpireAtUtc <= now
            })
            .ToListAsync();

        return Ok(subscriptions);
    }

    [HttpPost("extend-subscription")]
    public async Task<IActionResult> ExtendSubscription(
        [FromBody] ExtendSubscriptionRequest request)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(x =>
                x.UserEmail == request.UserEmail &&
                x.IsActive);

        if (subscription == null)
        {
            return NotFound();
        }

        if (subscription.ExpireAtUtc < DateTime.UtcNow)
        {
            subscription.ExpireAtUtc =
                DateTime.UtcNow.AddDays(request.Days);
        }
        else
        {
            subscription.ExpireAtUtc =
                subscription.ExpireAtUtc.AddDays(request.Days);
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            expireAtUtc = subscription.ExpireAtUtc
        });
    }

    [HttpPost("disable-subscription")]
    public async Task<IActionResult> DisableSubscription(
        [FromBody] DisableSubscriptionRequest request)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(x =>
                x.UserEmail == request.UserEmail &&
                x.IsActive);

        if (subscription == null)
        {
            return NotFound();
        }

        subscription.IsActive = false;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            userEmail = subscription.UserEmail,
            isActive = subscription.IsActive
        });
    }

    [HttpPost("enable-subscription")]
    public async Task<IActionResult> EnableSubscription(
        [FromBody] EnableSubscriptionRequest request)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(x =>
                x.UserEmail == request.UserEmail);

        if (subscription == null)
        {
            return NotFound();
        }

        subscription.IsActive = true;

        if (subscription.ExpireAtUtc < DateTime.UtcNow)
        {
            subscription.ExpireAtUtc =
                DateTime.UtcNow.AddDays(request.Days);
        }
        else
        {
            subscription.ExpireAtUtc =
                subscription.ExpireAtUtc.AddDays(request.Days);
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            userEmail = subscription.UserEmail,
            isActive = subscription.IsActive,
            expireAtUtc = subscription.ExpireAtUtc
        });
    }
}