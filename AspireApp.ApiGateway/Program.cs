using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using AspireApp.ApiGateway.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRandomLoadBalancer>();

var app = builder.Build();

await app.UseOcelot();

app.Run();