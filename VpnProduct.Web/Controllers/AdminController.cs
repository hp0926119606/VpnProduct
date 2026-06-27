using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Infrastructure.Data;
using VpnProduct.Web.Models;

using VpnProduct.Application.Interfaces;

namespace VpnProduct.Web.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

private readonly IWireGuardManager _wireGuardManager;


    public AdminController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
	IWireGuardManager wireGuardManager)
    {
        _db = db;
        _userManager = userManager;
 	_wireGuardManager = wireGuardManager;
    }

    private bool IsAdminAuthorized()
    {
        var token = Request.Headers["X-Admin-Token"].ToString();

        return token == "admin-123456";
    }

    [HttpGet("user-emails")]
    public async Task<IActionResult> GetUserEmails(
        [FromQuery] string search = "")
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        search = search.Trim();

        var users = await _userManager.Users
            .Where(x =>
                x.Email != null &&
                x.Email.Contains(search))
            .OrderBy(x => x.Email)
            .Take(20)
            .Select(x => x.Email!)
            .ToListAsync();

        return Ok(users);
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

        var data = await BuildUserRowsAsync(users);

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            data
        });
    }

    [HttpGet("users-export-xlsx")]
    public async Task<IActionResult> ExportUsersXlsx(
        [FromQuery] string? search = "")
    {
        if (!IsAdminAuthorized())
        {
            return Unauthorized();
        }

        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            usersQuery = usersQuery.Where(x =>
                x.Email != null &&
                x.Email.Contains(search));
        }

        var users = await usersQuery
            .OrderBy(x => x.Email)
            .ToListAsync();

        var rows = await BuildUserRowsAsync(users);

        using var stream = new MemoryStream();

        using (var spreadsheet = SpreadsheetDocument.Create(
            stream,
            SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

            var sheetData = new SheetData();

            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());

            sheets.Append(new Sheet
            {
                Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Users"
            });

            var header = new Row();

            AddCell(header, "Email");
            AddCell(header, "UserName");
            AddCell(header, "EmailConfirmed");
            AddCell(header, "SubscriptionActive");
            AddCell(header, "SubscriptionExpireAtUtc");
            AddCell(header, "IsExpired");
            AddCell(header, "PeerId");
            AddCell(header, "AssignedIp");
            AddCell(header, "PublicKey");
            AddCell(header, "PeerActive");

            sheetData.Append(header);

            foreach (var row in rows)
            {
                var excelRow = new Row();

                AddCell(excelRow, row.Email);
                AddCell(excelRow, row.UserName);
                AddCell(excelRow, row.EmailConfirmed.ToString());
                AddCell(excelRow, row.SubscriptionActive.ToString());
                AddCell(excelRow, row.SubscriptionExpireAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                AddCell(excelRow, row.IsExpired.ToString());
                AddCell(excelRow, row.PeerId?.ToString() ?? "");
                AddCell(excelRow, row.AssignedIp ?? "");
                AddCell(excelRow, row.PublicKey ?? "");
                AddCell(excelRow, row.PeerActive.ToString());

                sheetData.Append(excelRow);
            }

            workbookPart.Workbook.Save();
        }

        var bytes = stream.ToArray();

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "vpnproduct-users.xlsx");
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

    private async Task<List<AdminUserRow>> BuildUserRowsAsync(
        List<IdentityUser> users)
    {
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

            return new AdminUserRow
            {
                Id = user.Id,
                Email = email,
                UserName = user.UserName ?? "",
                EmailConfirmed = user.EmailConfirmed,
                SubscriptionActive = sub?.IsActive ?? false,
                SubscriptionExpireAtUtc = sub?.ExpireAtUtc,
                IsExpired = sub != null && sub.ExpireAtUtc <= DateTime.UtcNow,
                PeerId = peer?.Id,
                AssignedIp = peer?.AssignedIp,
                PublicKey = peer?.PublicKey,
                PeerActive = peer?.IsActive ?? false
            };
        }).ToList();

        return data;
    }

    private static void AddCell(Row row, string text)
    {
        row.Append(new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(text)
        });
    }

    private sealed class AdminUserRow
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public bool EmailConfirmed { get; set; }
        public bool SubscriptionActive { get; set; }
        public DateTime? SubscriptionExpireAtUtc { get; set; }
        public bool IsExpired { get; set; }
        public Guid? PeerId { get; set; }
        public string? AssignedIp { get; set; }
        public string? PublicKey { get; set; }
        public bool PeerActive { get; set; }
    }

[HttpPost("delete-user")]
public async Task<IActionResult> DeleteUser(
    [FromBody] DeleteUserRequest request)
{
    if (!IsAdminAuthorized())
    {
        return Unauthorized();
    }

    var email = request.Email.Trim();

    if (string.IsNullOrWhiteSpace(email))
    {
        return BadRequest(new
        {
            success = false,
            message = "Email is required."
        });
    }

    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
    {
        return NotFound(new
        {
            success = false,
            message = "User not found."
        });
    }

    var affectedNodeIds = await _db.VpnPeers
        .Where(x => x.Name == email)
        .Select(x => x.VpnNodeId)
        .Distinct()
        .ToListAsync();

    var peers = await _db.VpnPeers
        .Where(x => x.Name == email)
        .ToListAsync();

    var subscriptions = await _db.Subscriptions
        .Where(x => x.UserEmail == email)
        .ToListAsync();

    _db.VpnPeers.RemoveRange(peers);
    _db.Subscriptions.RemoveRange(subscriptions);

    var deleteUserResult =
        await _userManager.DeleteAsync(user);

    if (!deleteUserResult.Succeeded)
    {
        return Ok(new
        {
            success = false,
            message = string.Join("; ", deleteUserResult.Errors.Select(x => x.Description))
        });
    }

    var nodes = await _db.VpnNodes
        .Where(x => affectedNodeIds.Contains(x.Id))
        .ToListAsync();

    foreach (var node in nodes)
    {
        node.ConfigVersion += 1;
    }

    await _db.SaveChangesAsync();

    foreach (var nodeId in affectedNodeIds)
    {
        await _wireGuardManager.ApplyConfigurationAsync(nodeId);
    }

    return Ok(new
    {
        success = true,
        message = "User deleted.",
        email,
        deletedPeers = peers.Count,
        deletedSubscriptions = subscriptions.Count
    });
}

public sealed class DeleteUserRequest
{
    public string Email { get; set; } = "";
}

}