using Api.Gateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRandomLoadBalancer>((serviceProvider, _, discoveryProvider) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        return new WeightedRandomLoadBalancer(
            configuration,
            discoveryProvider!.GetAsync);
    });

builder.Configuration.AddOcelot();

var app = builder.Build();
await app.UseOcelot();
await app.RunAsync();
