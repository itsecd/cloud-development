using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using ProjectApp.Gateway.Configuration;
using ProjectApp.Gateway.LoadBalancer;
using ProjectApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var downstreamOverrides = BuildDownstreamOverrides(Environment.GetEnvironmentVariable("DOWNSTREAM_HOSTS"));
if (downstreamOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(downstreamOverrides);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [])
            .WithMethods("GET")
            .AllowAnyHeader();
    });
});

builder.Services.Configure<WeightedRoundRobinOptions>(
    builder.Configuration.GetSection(WeightedRoundRobinOptions.SectionName));

builder.Services.AddOcelot();
builder.Services.AddSingleton<ILoadBalancerCreator, WeightedRoundRobinCreator>();

var app = builder.Build();

app.UseCors("AllowClient");
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();

static Dictionary<string, string?> BuildDownstreamOverrides(string? downstreamHosts)
{
    var overrides = new Dictionary<string, string?>();
    if (string.IsNullOrWhiteSpace(downstreamHosts))
    {
        return overrides;
    }

    var entries = downstreamHosts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (var i = 0; i < entries.Length; i++)
    {
        var parts = entries[i].Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || !int.TryParse(parts[1], out var port))
        {
            continue;
        }

        var weight = 1;
        if (parts.Length >= 3)
        {
            int.TryParse(parts[2], out weight);
        }

        overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = parts[0];
        overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] = port.ToString();
        overrides[$"{WeightedRoundRobinOptions.SectionName}:Weights:{i}"] = (weight > 0 ? weight : 1).ToString();
    }

    return overrides;
}
