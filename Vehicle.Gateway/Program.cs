using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Vehicle.Gateway.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer<WeightedRandomLoadBalancer>((serviceProvider, _, discoveryProvider) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        if (discoveryProvider is null)
        {
            throw new InvalidOperationException("Ocelot service discovery provider is not available.");
        }

        return new WeightedRandomLoadBalancer(
            configuration,
            discoveryProvider.GetAsync);
    });

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Vehicle.Gateway",
    status = "ok",
    message = "Gateway is running"
}));

await app.UseOcelot();
await app.RunAsync();