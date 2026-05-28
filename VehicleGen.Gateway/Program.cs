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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET");
    });
});

var app = builder.Build();

app.UseCors();
await app.UseOcelot();

app.Run();