using Inventory.Gateway.LoadBalancer;
using Inventory.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration
    .AddJsonFile("ocelot.json", false, true);

builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRandom>((sp, route, discovery) =>
    {
        var logger = sp.GetRequiredService<ILogger<WeightedRandom>>();
        var services = discovery.GetAsync().GetAwaiter().GetResult().ToList();

        return new WeightedRandom(logger, services);
    });

builder.Services.AddCors(policy =>
{
    policy.AddPolicy("cors", cfg =>
    {
        cfg.AllowAnyOrigin()
           .WithMethods("GET")
           .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.UseCors("cors");

await app.UseOcelot();

app.Run();