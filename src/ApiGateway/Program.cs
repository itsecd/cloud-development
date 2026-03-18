using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Values;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var weights = new[] { 0.5, 0.3, 0.2 };

var replicaKeys = new[]
{
    "services__vehicleapi-1__http__0",
    "services__vehicleapi-2__http__0",
    "services__vehicleapi-3__http__0",
};

var replicaHosts = replicaKeys
    .Select(k => builder.Configuration[k])
    .Where(v => v != null)
    .Select(v => new Uri(v!))
    .Select(u => new ServiceHostAndPort(u.Host, u.Port))
    .ToList();

foreach (var (h, i) in replicaHosts.Select((h, i) => (h, i)))
    Console.WriteLine($"[Gateway] Replica {i + 1}: {h.DownstreamHost}:{h.DownstreamPort}");

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((route, discoveryProvider) =>
        new WeightedRandomLoadBalancer(
            replicaHosts.Count > 0 ? replicaHosts : route.DownstreamAddresses
                .Select(a => new ServiceHostAndPort(a.Host, a.Port))
                .ToList(),
            weights));

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();