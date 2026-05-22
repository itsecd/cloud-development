using Api.Gateway;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Patient.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer((sp, _, provider) =>
        new WeightedRoundRobin(provider.GetAsync, sp.GetRequiredService<IConfiguration>()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithHeaders("Content-Type")
            .WithMethods("GET");
    });
});

builder.AddServiceDefaults();
var app = builder.Build();

app.UseCors("AllowLocalDev");

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
