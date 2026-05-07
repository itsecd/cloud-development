using Api.Gateway;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using VehicleApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var overrides = new Dictionary<string, string?>();
var discovered = new List<string>();
for (var i = 0; Environment.GetEnvironmentVariable($"services__vehicleapp-api-{i}__https__0") is { } url; i++)
{
    var uri = new Uri(url);
    overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = uri.Host;
    overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] = uri.Port.ToString();
    discovered.Add($"{uri.Host}:{uri.Port}");
}

if (overrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(overrides);

builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRoundRobinLoadBalancer(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET")
              .WithHeaders("Content-Type")));

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

await app.UseOcelot();

app.Run();
