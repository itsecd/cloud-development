using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ResidentialBuilding.Gateway.LoadBalancer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddOcelot();

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((sp, _, discoveryProvider) =>
    {
        var logger = sp.GetRequiredService<ILogger<QueryBasedLoadBalancer>>();
        return new QueryBasedLoadBalancer(logger, discoveryProvider.GetAsync);
    });

var app = builder.Build();

app.UseCors("AllowLocalDev");
await app.UseOcelot();

await app.RunAsync();
