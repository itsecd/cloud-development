using Api.Gateway.Balancing;
using CloudDevelopment.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var overrides = new Dictionary<string, string?>();
for (var i = 0; Environment.GetEnvironmentVariable($"services__service-api-{i}__https__0") is { } url; i++)
{
    var uri = new Uri(url);
    overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = uri.Host;
    overrides[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] = uri.Port.ToString();
}

if (overrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(overrides);

builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRoundRobin(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyMethod();
    policy.AllowAnyHeader();
}));

builder.AddServiceDefaults();
var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
await app.UseOcelot();

app.Run();
