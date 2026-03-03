using CreditApp.Gateway.Extensions;
using CreditApp.Gateway.LoadBalancing;
using CreditApp.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var hostPortToServiceName = builder.ResolveDownstreamServices();

var replicaWeights = builder.Configuration
    .GetSection("ReplicaWeights")
    .Get<Dictionary<string, int>>() ?? [];

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((route, serviceDiscovery) =>
        new WeightedRoundRobinBalancer(serviceDiscovery, replicaWeights, hostPortToServiceName));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(allowedOrigins);
    });
});

var app = builder.Build();

app.UseCors("GatewayCors");
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
