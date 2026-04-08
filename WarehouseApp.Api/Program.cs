using WarehouseApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET")
              .AllowAnyHeader()));

builder.Services.AddScoped<IWarehouseItemService, WarehouseItemService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapGet("/api/warehouse-item", async (int id, IWarehouseItemService service) =>
    Results.Ok(await service.GetOrGenerate(id)));

app.Run();
