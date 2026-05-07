using Api.Gateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => origin.StartsWith("https://localhost"))
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((sp, _, discoveryProvider) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        return new WeightedRandomLoadBalancer(discoveryProvider.GetAsync, configuration);
    });

var app = builder.Build();

app.UseCors("LocalPolicy");
await app.UseOcelot();

await app.RunAsync();