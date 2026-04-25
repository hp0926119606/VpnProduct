using Microsoft.EntityFrameworkCore;
using VpnProduct.Application.Interfaces;
using VpnProduct.Infrastructure.Data;
using VpnProduct.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWireGuardServerConfigService, WireGuardServerConfigService>();
builder.Services.AddScoped<ISyncJobService, SyncJobService>();
builder.Services.AddScoped<IVpnPeerService, VpnPeerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
