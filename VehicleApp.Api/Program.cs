using VehicleApp.Api.Services;
using VehicleApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<IVehicleService, VehicleService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET")
              .WithHeaders("Content-Type")));

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapGet("/api/vehicle", async (int id, IVehicleService service) =>
    Results.Ok(await service.GetOrGenerateAsync(id)));

app.Run();
