using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ServiceDefaults;
using ApiGateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    }
    else
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
    }
});

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((serviceProvider, route, serviceDiscoveryProvider) =>
    {
        return new QueryBasedLoadBalancer(serviceDiscoveryProvider.GetAsync);
    });

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();

