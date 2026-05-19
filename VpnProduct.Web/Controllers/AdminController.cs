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

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    private bool IsAdminAuthorized()
    {
        var token =
            Request.Headers["X-Admin-Token"].ToString();

        return token == "admin-123456";
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