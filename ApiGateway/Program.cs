using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer(
        (route, serviceDiscoveryProvider) =>
            new WeightedRoundRobinBalancer(builder.Configuration));

var app = builder.Build();

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();