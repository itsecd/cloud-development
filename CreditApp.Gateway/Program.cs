using CreditApp.Gateway.LoadBalancers;
using CreditApp.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.AddOcelot();
builder.Services.AddSingleton<ILoadBalancerCreator, WeightedRoundRobinCreator>();

var app = builder.Build();

app.UseCors("wasm"); 
app.UseHttpsRedirection();
await app.UseOcelot();
app.Run();