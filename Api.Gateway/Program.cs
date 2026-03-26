using Api.Gateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRandomLoadBalancer(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

var trustedOrigins = builder.Configuration
    .GetSection("TrustedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(trustedOrigins)
              .WithMethods("GET")
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();
app.MapDefaultEndpoints();
await app.UseOcelot();
app.Run();
