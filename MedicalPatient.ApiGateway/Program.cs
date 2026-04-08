using MedicalPatient.ApiGateway.Balancer;
using MedicalPatient.AppHost.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
    ["https://localhost:7282", "http://localhost:5127"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type")
        .AllowCredentials();
    });
});

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var generators = builder.Configuration.GetSection("Generators").Get<string[]>() ?? [];

var configuredWeights = builder.Configuration
    .GetSection("Routes:0:DownstreamHostWeights")
    .GetChildren()
    .Select(x => x.GetValue("Weight", 1))
    .Where(w => w > 0)
    .ToArray();
var weights = configuredWeights.Length > 0 ? configuredWeights : [3, 2, 1];

var addressOverrides = new List<KeyValuePair<string, string?>>();

for (var i = 0; i < generators.Length; ++i)
{
    var name = generators[i];
    var url = builder.Configuration[$"services:{name}:http:0"];

    if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        addressOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Host", uri.Host));
        addressOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Port", uri.Port.ToString()));
    }
}

if (addressOverrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(addressOverrides);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRoundRobin>((route, serviceDiscovery) =>
        new WeightedRoundRobin(serviceDiscovery, weights));

var app = builder.Build();

app.UseCors("ClientCorsPolicy");

await app.UseOcelot();
await app.RunAsync();