using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Application.Interfaces;
using VpnProduct.Infrastructure.Data;

namespace VpnProduct.Web.Controllers;

[ApiController]
[Route("api/admin/devices")]
public class DevicesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWireGuardManager _wireGuardManager;

    public DevicesController(
        ApplicationDbContext db,
        IWireGuardManager wireGuardManager)
    {
        _db = db;
        _wireGuardManager = wireGuardManager;
    }

    private bool IsAdminAuthorized()
    {
        var token = Request.Headers["X-Admin-Token"].ToString();

        return token == "admin-123456";
    }

    [HttpGet]
    public async Task<IActionResult> GetDevices(
        [FromQuery] string email = "")
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        email = email.Trim();

        var query = _db.UserDevices
            .AsNoTracking()
            .Include(x => x.VpnPeer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(x => x.UserEmail == email);
        }

        var data = await query
            .OrderBy(x => x.UserEmail)
            .ThenBy(x => x.DeviceName)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.UserEmail,
                x.DeviceName,
                x.DeviceType,
                x.DeviceIdentifier,
                x.VpnPeerId,
                AssignedIp = x.VpnPeer == null ? "" : x.VpnPeer.AssignedIp,
                PeerActive = x.VpnPeer != null && x.VpnPeer.IsActive,
                x.IsActive,
                x.CreatedAtUtc,
                x.LastSeenAtUtc
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("disable")]
    public async Task<IActionResult> DisableDevice(
        [FromBody] DeviceStatusRequest request)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var device = await _db.UserDevices
            .Include(x => x.VpnPeer)
            .FirstOrDefaultAsync(x => x.Id == request.DeviceId);

        if (device == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Device not found."
            });
        }

        device.IsActive = false;

        if (device.VpnPeer != null)
        {
            device.VpnPeer.IsActive = false;
        }

        await _db.SaveChangesAsync();

        if (device.VpnPeer != null)
        {
            await _wireGuardManager.ApplyConfigurationAsync(device.VpnPeer.VpnNodeId);
        }

        return Ok(new
        {
            success = true,
            message = "Device disabled.",
            device.Id,
            device.UserEmail,
            device.DeviceName
        });
    }

    [HttpPost("enable")]
    public async Task<IActionResult> EnableDevice(
        [FromBody] DeviceStatusRequest request)
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var device = await _db.UserDevices
            .Include(x => x.VpnPeer)
            .FirstOrDefaultAsync(x => x.Id == request.DeviceId);

        if (device == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Device not found."
            });
        }

        device.IsActive = true;

        if (device.VpnPeer != null)
        {
            device.VpnPeer.IsActive = true;
        }

        await _db.SaveChangesAsync();

        if (device.VpnPeer != null)
        {
            await _wireGuardManager.ApplyConfigurationAsync(device.VpnPeer.VpnNodeId);
        }

        return Ok(new
        {
            success = true,
            message = "Device enabled.",
            device.Id,
            device.UserEmail,
            device.DeviceName
        });
    }
}

public sealed class DeviceStatusRequest
{
    public Guid DeviceId { get; set; }
}