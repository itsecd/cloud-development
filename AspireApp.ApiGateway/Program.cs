using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.LoadBalancer.Interfaces;
using AspireApp.ApiGateway.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:5127")
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();
builder.Services.AddSingleton<ILoadBalancerFactory, WeightedRandomLoadBalancerFactory>();

var app = builder.Build();

app.UseCors("AllowClient");

await app.UseOcelot();

app.Run();