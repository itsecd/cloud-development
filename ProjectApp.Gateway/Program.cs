using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using ProjectApp.Gateway.LoadBalancing;
using ProjectApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.AddServiceDefaults();

builder.Services.AddSingleton<ILoadBalancerCreator, QueryBasedLoadBalancerCreator>();
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
await app.UseOcelot();

app.Run();
