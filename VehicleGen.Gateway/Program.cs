using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using VehicleGen.Gateway.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRandomLoadBalancer>((serviceProvider, route, serviceDiscoveryProvider) =>
        new WeightedRandomLoadBalancer(
            async () =>
            {
                var services = await serviceDiscoveryProvider.GetAsync();
                return services.ToList();
            },
            serviceProvider.GetRequiredService<IConfiguration>()
        ));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
await app.UseOcelot();

app.Run();