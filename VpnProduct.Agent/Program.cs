using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VpnProduct.Agent.Models;
using VpnProduct.Agent.Services;

var builder = Host.CreateApplicationBuilder(args);

var options = new AgentOptions();
builder.Configuration.GetSection("Agent").Bind(options);

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<WireGuardConfigMergeService>();
builder.Services.AddSingleton<WireGuardApplyService>();

builder.Services.AddHttpClient<AgentApiClient>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl.EndsWith("/") ? options.BaseUrl : options.BaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<VpnProduct.Agent.Worker>();

var host = builder.Build();
host.Run();
