using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using WarehouseItem.Gateway.LoadBalancer;
using WarehouseItem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "LocalDev";

builder.AddServiceDefaults();

builder.Services.AddCors(static options =>
{
    options.AddPolicy(CorsPolicyName, static policy =>
        policy.AllowAnyOrigin()
            .WithHeaders("Content-Type")
            .WithMethods("GET"));
});

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddOcelot();

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((sp, _, discoveryProvider) =>
        new QueryBasedLoadBalancer(
            sp.GetRequiredService<ILogger<QueryBasedLoadBalancer>>(),
            discoveryProvider.GetAsync));

var app = builder.Build();

app.UseCors(CorsPolicyName);

await app.UseOcelot();
await app.RunAsync();
