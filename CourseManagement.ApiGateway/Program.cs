using CourseManagement.ApiGateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Ocelot config
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot with Query Load Balancer
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<QueryLoadBalancer>((serviceProvider, downstreamRoute, serviceDiscoveryProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QueryLoadBalancer>>();

        var services = serviceDiscoveryProvider.GetAsync().GetAwaiter().GetResult().ToList();

        return new QueryLoadBalancer(logger, services);
    });

var app = builder.Build();

// Use Ocelot
await app.UseOcelot();

app.Run();