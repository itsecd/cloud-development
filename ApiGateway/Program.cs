using ApiGateway.LoadBalancing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Передаём IConfiguration в балансировщик через лямбду
builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer(() => new WeightedRoundRobinBalancer(builder.Configuration));

var app = builder.Build();

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();