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
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;

        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IVpnPeerService, VpnPeerService>();
builder.Services.AddScoped<ISyncJobService, SyncJobService>();
builder.Services.AddScoped<IWireGuardServerConfigService, WireGuardServerConfigService>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();