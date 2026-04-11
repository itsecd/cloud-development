using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using PatientApp.Gateway.LoadBalancer;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();

builder.Services.AddSingleton<ILoadBalancerCreator, QueryBasedLoadBalancerCreator>();

var app = builder.Build();

await app.UseOcelot();

app.Run();