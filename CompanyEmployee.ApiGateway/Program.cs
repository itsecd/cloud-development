using CompanyEmployee.ApiGateway.LoadBalancer;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using Ocelot.Values;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

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

var app = builder.Build();

await app.UseOcelot();

app.Run();