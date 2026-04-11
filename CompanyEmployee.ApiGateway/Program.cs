using CompanyEmployee.ApiGateway.LoadBalancer;
using CompanyEmployee.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Values;

var builder = WebApplication.CreateBuilder(args).AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var generators = builder.Configuration.GetSection("Generators").Get<string[]>() ?? [];
var overrides = new List<KeyValuePair<string, string?>>();

for (var i = 0; i < generators.Length; i++)
{
    var serviceName = generators[i];
    var url = builder.Configuration[$"services:{serviceName}:http:0"];

    Console.WriteLine($"{serviceName} -> {url}");

    if (string.IsNullOrWhiteSpace(url))
        continue;

    var uri = new Uri(url);

    overrides.Add(new KeyValuePair<string, string?>(
        $"Routes:0:DownstreamHostAndPorts:{i}:Host", uri.Host));

    overrides.Add(new KeyValuePair<string, string?>(
        $"Routes:0:DownstreamHostAndPorts:{i}:Port", uri.Port.ToString()));
}

builder.Configuration.AddInMemoryCollection(overrides);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((route, _) =>
    {
        var config = builder.Configuration;

        var services = route.DownstreamAddresses
            .Select(h => new ServiceHostAndPort(h.Host, h.Port))
            .ToList();

        return new WeightedRoundRobin(services, config);
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.WithOrigins("https://localhost:7282")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("wasm");

await app.UseOcelot();

app.Run();