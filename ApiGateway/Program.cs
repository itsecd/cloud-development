using ApiGateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7282",
            "http://localhost:7282")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOcelot();
builder.Services.AddSingleton < ILoadBalancerCreator, WeightedRoundRobinCreator > ();

var app = builder.Build();

app.UseCors("AllowClient");

await app.UseOcelot();

app.Run();