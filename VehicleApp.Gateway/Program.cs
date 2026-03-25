using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using VehicleApp.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<QueryBased>((_, _, provider) => new(provider.GetAsync));

builder.Services.AddCors(options =>
    options.AddPolicy("AllowClient", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [])
              .WithMethods("GET")
              .AllowAnyHeader()));

var app = builder.Build();

app.UseCors("AllowClient");

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
