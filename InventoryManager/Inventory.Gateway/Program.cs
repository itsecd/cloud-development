using Inventory.Gateway.LoadBalancer;
using Inventory.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot()
    .AddCustomLoadBalancer<RandomSelector>((provider, route, discovery) =>
    {
        var log = provider.GetRequiredService<ILogger<RandomSelector>>();

        var downstream = discovery
            .GetAsync()
            .GetAwaiter()
            .GetResult()
            .ToList();

        return new RandomSelector(log, downstream);
    });

builder.Services.AddCors(policy =>
{
    policy.AddPolicy("ClientPolicy", cfg =>
    {
        cfg.AllowAnyOrigin()
           .WithMethods("GET")
           .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.UseCors("ClientPolicy");

await app.UseOcelot();

app.Run();