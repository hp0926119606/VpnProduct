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

    [HttpPost("extend-subscription")]
    public async Task<IActionResult> ExtendSubscription(
        [FromBody] ExtendSubscriptionRequest request)
    {
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
            subscription.ExpireAtUtc = DateTime.UtcNow.AddDays(request.Days);
        }
        else
        {
            subscription.ExpireAtUtc = subscription.ExpireAtUtc.AddDays(request.Days);
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