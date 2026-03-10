using CompanyEmployee.Gateway.LoadBalancers;
using CompanyEmployee.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.LoadBalancer.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", false, true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

builder.Services.AddOcelot()
    .AddPolly();

builder.Services.AddSingleton<ILoadBalancerFactory, WeightedRandomLoadBalancerFactory>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors("wasm");
app.UseHttpsRedirection();

await app.UseOcelot();

app.Run();