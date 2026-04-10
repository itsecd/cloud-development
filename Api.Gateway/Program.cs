using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Api.Gateway.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRandomLoadBalancer(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

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

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
