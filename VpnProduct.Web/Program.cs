using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Application.Interfaces;
using VpnProduct.Infrastructure.Data;
using VpnProduct.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IVpnPeerService, VpnPeerService>();
builder.Services.AddScoped<ISyncJobService, SyncJobService>();
builder.Services.AddScoped<IWireGuardServerConfigService, WireGuardServerConfigService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
